using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WebSocketSharp;

public class WSConnectionController : Singleton<WSConnectionController>
{
    [System.Serializable]
    private class CYAPlayerInfo
    {
        public string role;
        public string spotId;
        public string username;
        public string playerId;
        public HashSet<string> commandHandlers;
        public string token;

        public CYAPlayerInfo(bool _isMod, string _spotId, string _usename, string _playerId)
        {
            this.role = _isMod ? "_cya_mod" : "_cya_spec";
            this.spotId = _spotId;
            this.username = _usename;
            this.playerId = _playerId;
            this.token = string.Empty;
        }
    }

    [System.Serializable]
    private class Payload
    {
        public string command;
        public string key;
        public string val;
        public string timestamp;
        public string player;
        public string playername;
    }

    [System.Serializable]
    private class TokenResult
    {
        public string token;
        public string auth;        
    }

    [System.Serializable]
    private class SendCommandRequest
    {
        public string command;
        public string key;
        public string val;
    }

    [System.Serializable]
    private class SyncResponse
    {
        public string appKey;
        public string command;
        public Payload payload; 
    }

    [System.Serializable]
    private class ResponseReceived : UnityEvent<Payload> { }

    private const string HttpURL = "https://open.cyalive.co";
    private const string SocketURL = "wss://opensocket.cyalive.co";

    private WebSocket mWebSocket = null;
    private string WebSocketConnectionToken = string.Empty;
    private CYAPlayerInfo cyaPlayer = null;

    private ResponseReceived syncResponseReceived;

    // Start is called before the first frame update
    private void Start()
    {
        this.cyaPlayer = new CYAPlayerInfo(true, "aasjdhjkj123", "zzh_test", System.Guid.NewGuid().ToString());
        string JSONString = JsonUtility.ToJson(this.cyaPlayer);
        StartCoroutine(PostRequest(HttpURL + "/v2/gen", JSONString));

        this.syncResponseReceived = new ResponseReceived();
        this.syncResponseReceived.AddListener(HandleResponse);
    }

    private IEnumerator PostRequest(string url, string body, string token = "0")
    {
        UnityWebRequest unityWebRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        unityWebRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(new System.Text.UTF8Encoding().GetBytes(body));
        unityWebRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        unityWebRequest.SetRequestHeader("Authorization", token);

        //Send the request then wait here until it return
        yield return unityWebRequest.SendWebRequest();

        if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error while sending connection request: " + unityWebRequest.error);
        }
        else
        {
            TokenResult result = JsonUtility.FromJson<TokenResult>(unityWebRequest.downloadHandler.text);
            this.cyaPlayer.token = "bearer " + result.auth;
            this.WebSocketConnectionToken = result.token;
            this.mWebSocket = new WebSocket(SocketURL + "/ws?token=" + this.WebSocketConnectionToken);

            this.mWebSocket.OnOpen += (sender, e) =>
            {
                Debug.Log("Socket opened");
            };
            this.mWebSocket.OnMessage += (sender, e) =>
            {
                // catch sync message
                Debug.Log(" Data : " + e.Data);
                SyncResponse response = JsonUtility.FromJson<SyncResponse>(e.Data);
                syncResponseReceived.Invoke(response.payload);
            };

            this.mWebSocket.Connect();

            // Send start a game message

            // Keep connected
            StartCoroutine(KeepPinging());

        }
        unityWebRequest.Dispose();
    }

    private IEnumerator KeepPinging()
    {
        while(true)
        {
            this.mWebSocket.Send("Ping");
            yield return new WaitForSeconds(2.5f);
        }
    }

    private IEnumerator SyncRequest(string url, string body, string token = "0")
    {
        UnityWebRequest unityWebRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        unityWebRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(new System.Text.UTF8Encoding().GetBytes(body));
        unityWebRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        unityWebRequest.SetRequestHeader("Authorization", token);
        yield return unityWebRequest.SendWebRequest();

        if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + unityWebRequest.error);
        }
        unityWebRequest.Dispose();
    }

    public void SyncDrawing(DrawingController.DrawingModes _mode)
    {
        if (_mode != DrawingController.DrawingModes.EDRAW_CLEAN)
        {
            return;
        }

        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = "draw_clean";
        newCommand.key = "cleanup";
        newCommand.val = "0";
        StartCoroutine(SyncRequest(HttpURL + "/v2/bc/set", JsonUtility.ToJson(newCommand), this.cyaPlayer.token));
    }

    public void SyncDrawing(DrawingController.DrawingModes _mode, Vector2 _position)
    {
        if (_mode == DrawingController.DrawingModes.EDRAW_CLEAN)
        {
            return;
        }

        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = _mode == DrawingController.DrawingModes.EDRAW_CREATE ? "draw_create" : "draw_update";
        newCommand.key = "coordinates";
        newCommand.val = _position.x + "," + _position.y;
        StartCoroutine(SyncRequest(HttpURL + "/v2/bc/set", JsonUtility.ToJson(newCommand), this.cyaPlayer.token));
    }


    private void HandleResponse(Payload _payload)
    {
        /*if (_payload.player == this.cyaPlayer.playerId)
        {
            return;
        }*/
        if (_payload.key != "coordinates" && _payload.key != "cleanup")
        {
            Debug.Log("Wrong key");
            return;
        }
        string[] coordinates = _payload.val.Split(',');
        Vector2 point = new Vector2(System.Convert.ToSingle(coordinates[0]), System.Convert.ToSingle(coordinates[1]));

        switch (_payload.command)
        {
            case "draw_create":
                DrawingController.Instance.SyncCreateLine(point);
                break;
            case "draw_update":
                DrawingController.Instance.SyncUpdateLine(point);
                break;
            case "draw_clean":
                DrawingController.Instance.SyncCleanUp();
                break;
            default:
                Debug.Log("Invalid command");
                break;
        }
    }
}
