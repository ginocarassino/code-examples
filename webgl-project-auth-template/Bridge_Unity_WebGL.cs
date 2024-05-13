using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class Bridge_Unity_WebGL : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShowMessage(string message);
#endif

    public Text T_token;
    public Text T_id;

    public InputField T_input;

    void Start()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        WebGLInput.captureAllKeyboardInput = false;
#endif
    }

    //This function calls JavaScript function to display alert message
    public void SendToJS()
    {
        string MessageToSend = "Message . .";
        Debug.Log("Sending message to JavaScript: " + MessageToSend);

#if UNITY_WEBGL && !UNITY_EDITOR
        //this line calls Javascript function that is stored in "Plugins" folder
        ShowMessage(MessageToSend);
#endif
    }

    //These two functions receive message sent by web browser (Javascript)
    public void getAuthTokenID(string message)
    {
        if (message != null)
        {
            Debug.Log("TOKEN OK: " + message);
        }
        else
        {
            Debug.Log("Token is NULL");
        }
    }
    public void getUserID(string message)
    {
        if (message != null)
        {
            Debug.Log("USER ID OK: " + message);
        }
        else
        {
            Debug.Log("USER ID is NULL");
        }
    }
}
