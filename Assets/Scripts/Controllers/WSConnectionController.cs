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

    public enum ConnectionEnv
    {
        EProduction,
        EDev,
    }

    // CONST
    private const string DevHttpURL = "https://open.cyalive.co";
    private const string HttpURL = "https://open.cya.live";
    private const string SocketURL = "wss://opensocket.cyalive.co";
    
    // ENV
    public ConnectionEnv mEnv = ConnectionEnv.EDev;

    // Connection
    private WebSocket mWebSocket = null;
    private CYAPlayerInfo cyaPlayer = null;
    
    // Development
    private string mWebSocketConnectionToken = string.Empty;
    // Production
    private string mProductionToken = string.Empty;

    // Data / Payload
    private Queue<Payload> mPayloadQueue;
    
    // Runtime 
    private bool mIsConnectionReady = false;
    public UnityEvent ConnectionStatusChanged = new UnityEvent();


    // Start is called before the first frame update
    private void Start()
    {
        this.mPayloadQueue = new Queue<Payload>();

        this.cyaPlayer = new CYAPlayerInfo(true, "aasjdhjkj123", "zzh_test", System.Guid.NewGuid().ToString());

        if (this.mEnv == ConnectionEnv.EDev)
        {
            string JSONString = JsonUtility.ToJson(this.cyaPlayer);
            StartCoroutine(PostRequest(DevHttpURL + "/v2/gen", JSONString));
            this.ConnectionStatusChanged.Invoke();
        }
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
        this.mProductionToken = "bearer " + _token;

        if (this.mProductionToken != string.Empty)
        {
            this.mEnv = ConnectionEnv.EProduction;
        }
        this.mIsConnectionReady = true;
        this.ConnectionStatusChanged.Invoke();
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
            // Production [currently not using]
            if (this.mEnv == ConnectionEnv.EProduction)
            {
                this.cyaPlayer.token = "bearer " + this.mProductionToken;
            }

            // Development connection
            else if (this.mEnv == ConnectionEnv.EDev)
            {
                TokenResult result = JsonUtility.FromJson<TokenResult>(unityWebRequest.downloadHandler.text);
                this.cyaPlayer.token = "bearer " + result.auth;
                this.mWebSocketConnectionToken = result.token;

                this.mWebSocket = new WebSocket(SocketURL + "/ws?token=" + this.mWebSocketConnectionToken);

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
            }
        }
        unityWebRequest.Dispose();
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

        string url = this.mEnv == ConnectionEnv.EDev ? DevHttpURL : HttpURL;
        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = "draw_clean";
        newCommand.key = "cleanup";
        newCommand.val = "[0,0]";
        StartCoroutine(SyncRequest(url + "/v2/bc/set", JsonUtility.ToJson(newCommand), this.mEnv == ConnectionEnv.EProduction ? this.mProductionToken : this.cyaPlayer.token));
    }

    public void SyncColor(DrawingController.DrawingModes _mode, string _color)
    {
        if (_mode != DrawingController.DrawingModes.EDRAW_CHANGECOLOR)
        {
            return;
        }

        string url = this.mEnv == ConnectionEnv.EDev ? DevHttpURL : HttpURL;
        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = "draw_change_color";
        newCommand.key = "color";
        newCommand.val = _color;
        StartCoroutine(SyncRequest(url + "/v2/bc/set", JsonUtility.ToJson(newCommand), this.mEnv == ConnectionEnv.EProduction ? this.mProductionToken : this.cyaPlayer.token));
    }

    // Draw sync
    public void SyncDrawing(DrawingController.DrawingModes _mode, List<Vector2> _points, string _color)
    {
        if (_mode != DrawingController.DrawingModes.EDRAW_CREATE)
        {
            return;
        }

        string url = this.mEnv == ConnectionEnv.EDev ? DevHttpURL : HttpURL;
        SendCommandRequest newCommand = new SendCommandRequest();
        newCommand.command = "draw_create";
        newCommand.key = "lines";
        newCommand.val = "[" + _color + "]";

        foreach (Vector2 point in _points)
        {
            newCommand.val += "[" + point.x + "," + point.y + "]";
        }

        StartCoroutine(SyncRequest(url + "/v2/bc/set", JsonUtility.ToJson(newCommand), this.mEnv == ConnectionEnv.EProduction ? this.mProductionToken : this.cyaPlayer.token));
    }

    // Handle messages
    private void HandleResponse(Payload _payload)
    {
        if (_payload.player == this.cyaPlayer.playerId)
        {
            return;
        }
        if (_payload.key != "lines" && _payload.key != "cleanup" && _payload.key != "color")
        {
            Debug.Log("Wrong key");
            return;
        }

        string colorString = string.Empty;
        List<Vector2> points = new List<Vector2>();
        if (_payload.key == "lines")
        {
            string[] coordinates = _payload.val.Split('[', ']');
            foreach (string coordinate in coordinates)
            {
                if (coordinate.Length == 0)
                {
                    continue;
                }
                if (coordinate.Contains(','))
                {
                    string[] pointString = coordinate.Split(',');
                    points.Add(new Vector2(System.Convert.ToSingle(pointString[0]), System.Convert.ToSingle(pointString[1])));
                }
                else
                {
                    colorString = coordinate;
                }
            }
        }

        switch (_payload.command)
        {
            case "draw_create":
                DrawingController.Instance.SyncCreateLine(points, colorString);
                break;
            case "draw_clean":
                DrawingController.Instance.SyncCleanUp();
                break;
            case "draw_change_color":
                DrawingController.Instance.SyncColor(_payload.val);
                break;
            default:
                Debug.Log("Invalid command");
                break;
        }
    }
}
