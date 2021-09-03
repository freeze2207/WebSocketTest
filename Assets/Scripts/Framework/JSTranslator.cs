using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSTranslator : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void SayHello();

    [DllImport("__Internal")]
    private static extern string ReportReady();

    [DllImport("__Internal")]
    private static extern string GetWindowURL();

    // Start is called before the first frame update
    void Start()
    {
        ReportReady();
        string search = GetWindowURL();
        if (search != string.Empty)
        {
            // Debug message
            string token = search.Split('=')[1];
            WSConnectionController.Instance.text.text = token;

            this.GetToken(token);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Q))
        {
            SayHello();
        }
    }

    public void GetSyncData(string _data)
    {
        WSConnectionController.Instance.GetSyncData(_data);
        WSConnectionController.Instance.text.text = _data;
    }

    public void GetToken(string _token)
    {
        WSConnectionController.Instance.SetToken(_token);
    }
}
