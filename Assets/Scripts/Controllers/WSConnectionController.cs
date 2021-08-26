
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

public class WSConnectionController : Singleton<WSConnectionController>
{
    [System.Serializable]
    private class CYAPlayerInfo
    {
        public string role;
        public string spotID;
        public string username;
        public string playerID;
        public HashSet<string> commandHandlers;
        public string token;
    }

    [System.Serializable]
    private class Payload
    {
        string[] group;
    }

    [System.Serializable]
    private class TokenResult
    {
        public Payload payload;
        public string token;
        public string auth;        
    }

    private const string HttpURL = "https://open.cyalive.co";
    private const string SocketURL = "wss://opensocket.cyalive.co";

    private WebSocket webSocket = null;
    private string WebSocketConnectionToken = string.Empty;
    private CYAPlayerInfo cyaPlayer = new CYAPlayerInfo();

    // Start is called before the first frame update
    private void Start()
    {
        string JSONString = JsonUtility.ToJson(new TokenResult());
        StartCoroutine(PostRequest(HttpURL + "/v2/gen", JSONString));
    }

    // Update is called once per frame
    private void Update()
    {
        // DEBUG USE
        if (webSocket == null)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            webSocket.Send("Hello");
        }
    }

    IEnumerator PostRequest(string url, string json)
    {
        UnityWebRequest unityWebRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        unityWebRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        unityWebRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        unityWebRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        unityWebRequest.SetRequestHeader("Authorization", "0");

        //Send the request then wait here until it return
        yield return unityWebRequest.SendWebRequest();

        if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + unityWebRequest.error);
        }
        else
        {
            TokenResult result = JsonUtility.FromJson<TokenResult>(unityWebRequest.downloadHandler.text);
            this.cyaPlayer.token = "bearer " + result.auth;
            this.WebSocketConnectionToken = result.token;

            webSocket = new WebSocket(SocketURL + "/ws?token=" + this.WebSocketConnectionToken);
            
            webSocket.OnOpen += (sender, e) =>
            {
                Debug.Log("Socket opened");
            };
            webSocket.OnMessage += (sender, e) =>
            {
                Debug.Log("Message Received from " + ((WebSocket)sender).Url + ", Data : " + e.Data);
            };

            webSocket.Connect();

        }
    }
}
