using gamingCloud.Network.Voip;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoipClientDisconnection : MonoBehaviour
{

    private void OnDestroy()
    {
        GCVoip.Disconnect();
    }

    private void OnApplicationQuit()
    {
        if (Application.isEditor)
        {
            GCVoip.Disconnect();
        }
    }
    
}
