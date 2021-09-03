using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
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

    private enum ConnectionEnv
    {
        EProduction,
        EDev,
    }

    private const string HttpURL = "https://open.cyalive.co";
    private const string SocketURL = "wss://opensocket.cyalive.co";

    private ConnectionEnv mEnv = ConnectionEnv.EDev;

    private WebSocket mWebSocket = null;
    private string WebSocketConnectionToken = string.Empty;
    private CYAPlayerInfo cyaPlayer = null;

    private Queue<Payload> mPayloadQueue;

    private string mProductionToken = string.Empty;
    private bool mIsConnectionReady = false;

    // Debug use
    public Text text;

    // Start is called before the first frame update
    private void Start()
    {
        this.mPayloadQueue = new Queue<Payload>();

        this.cyaPlayer = new CYAPlayerInfo(true, "aasjdhjkj123", "zzh_test", System.Guid.NewGuid().ToString());

    }

    private void Update()
    {
        if (this.mPayloadQueue.Count != 0)
        {
            lock (this.mPayloadQueue)
            {
                this.HandleResponse(this.mPayloadQueue.Dequeue());
            }
        }
    }

    public void SetToken(string _token)
    {
        this.mProductionToken = _token;

        if (this.mProductionToken != string.Empty)
        {
            this.mEnv = ConnectionEnv.EProduction;
            /*string JSONString = JsonUtility.ToJson(this.cyaPlayer);
            StartCoroutine(PostRequest(HttpURL + "/v2/gen", JSONString));*/

        }
        this.mIsConnectionReady = true;
    }

    private IEnumerator PostRequest(string url, string body, string token = "0")
    {
        UnityWebRequest unityWebRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        unityWebRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(new System.Text.UTF8Encoding().GetBytes(body));
        unityWebRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        unityWebRequest.SetRequestHeader("Content-Type", "application/json");
        unityWebRequest.SetRequestHeader("Authorization", token);
        this.text.text = "before send,";
        //Send the request then wait here until it return
        yield return unityWebRequest.SendWebRequest();

        this.text.text += " received,";

        if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error while sending connection request: " + unityWebRequest.error);
            this.text.text += " connection error,";
        }
        else
        {
            // Production
            if (this.mEnv == ConnectionEnv.EProduction)
            {
                this.cyaPlayer.token = "bearer " + this.mProductionToken;
            }

            // Development connection
            else if (this.mEnv == ConnectionEnv.EDev)
            {
                TokenResult result = JsonUtility.FromJson<TokenResult>(unityWebRequest.downloadHandler.text);
                this.cyaPlayer.token = "bearer " + result.auth;
                this.WebSocketConnectionToken = result.token;

                this.mWebSocket = new WebSocket(SocketURL + "/ws?token=" + this.WebSocketConnectionToken);

                this.text.text += " before WS connect,";
                this.text.text = this.WebSocketConnectionToken;

                this.mWebSocket.OnOpen += (sender, e) =>
                {
                    Debug.Log("Socket opened");
                };
                this.mWebSocket.OnMessage += (sender, e) =>
                {
                    // catch sync message
                    Debug.Log(" Data : " + e.Data);
                    this.GetSyncData(e.Data);
                };

                this.mWebSocket.Connect();
                // Send start a game message

                this.text.text += " WS after con,";

                // Keep connected
                //StartCoroutine(KeepPinging());
            }
        }
        unityWebRequest.Dispose();
    }

    /*private IEnumerator KeepPinging()
    {
        while(true)
        {
            this.mWebSocket.Send("Ping");
            yield return new WaitForSeconds(2.5f);
        }
    }*/

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

    public void GetSyncData(string _dataString)
    {
        SyncResponse response = JsonUtility.FromJson<SyncResponse>(_dataString);

        lock (this.mPayloadQueue)
        {
            this.mPayloadQueue.Enqueue(response.payload);
        }
    }

    public bool GetConnectionStatus()
    {
        return this.mIsConnectionReady;
    }

    // Clean up sync
    public void SyncDrawing(DrawingController.DrawingModes _mode)
    {
        if (_mode != DrawingController.DrawingModes.EDRAW_CLEAN)
        {
            return;
        }

        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = "draw_clean";
        newCommand.key = "cleanup";
        newCommand.val = "0,0";
        StartCoroutine(SyncRequest(HttpURL + "/v2/bc/set", JsonUtility.ToJson(newCommand), this.cyaPlayer.token));
    }

    // Draw sync
    public void SyncDrawing(DrawingController.DrawingModes _mode, List<Vector2> _points)
    {
        if (_mode != DrawingController.DrawingModes.EDRAW_CREATE)
        {
            return;
        }

        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = "draw_create";
        newCommand.key = "coordinates";
        foreach (Vector2 point in _points)
        {
            newCommand.val += "[" + point.x + "," + point.y + "]";
        }

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

        List<Vector2> points = new List<Vector2>();
        string[] coordinates = _payload.val.Split('[', ']');
        foreach (string coordinate in coordinates)
        {
            if (coordinate.Length == 0)
            {
                continue;
            }
            string[] pointString = coordinate.Split(',');
            points.Add(new Vector2(System.Convert.ToSingle(pointString[0]), System.Convert.ToSingle(pointString[1])));
        }

        switch (_payload.command)
        {
            case "draw_create":
                DrawingController.Instance.SyncCreateLine(points);
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
