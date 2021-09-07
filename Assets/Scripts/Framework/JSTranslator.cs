using System;
using System.Web;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;


public class JSTranslator : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string ReportReady();

    [DllImport("__Internal")]
    private static extern string GetWindowURLToken();

    // Start is called before the first frame update
    void Start()
    {
        ReportReady();
        string token = GetWindowURLToken();
        if (token != string.Empty)
        {
            this.GetToken(token);
        }
    }

    public void GetSyncData(string _data)
    {
        WSConnectionController.Instance.GetSyncData(_data);
    }

    public void GetToken(string _token)
    {
        WSConnectionController.Instance.SetToken(_token);
    }
}
