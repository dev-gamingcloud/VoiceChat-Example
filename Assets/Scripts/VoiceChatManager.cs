using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using gamingCloud;
using gamingCloud.Network.Voip;
using UnityEngine.UI;

public class VoiceChatManager : MonoBehaviour
{

    [Header("Connection UI Elements")]
    public GameObject ConnectionPanel;
    public InputField displayName_field, roomName_field;

    [Header("Room UI Elements")]
    public GameObject RoomPanel, Holder;
    public GameObject DisplayClient;

    bool connected = false;

    private async void Start()
    {
        // create guest player for ab/test
        //if(await Players.IsDeviceLogedIn() == false)
        //    await Players.CreateGuestPlayer();

        GCVoip.OnChannelChanged += GCVoip_OnChannelChanged;
        Application.runInBackground = true;

        ConnectionPanel.SetActive(true);
        RoomPanel.SetActive(false);
    }

    private void GCVoip_OnChannelChanged(VoipClientInfo obj)
    {
        Debug.Log("info : "+obj.displayName);

    }

    //public void ChangeChannelCOlor(Button btn)
    //{
    //    btn.image.color = new Color(28, 72, 154);
    //}

    public void ChangeChannel(int channel)
    {
        GCVoip.ChangeChannel((GCChannels)channel);
        Debug.Log("channel changed to " + (GCChannels)channel);

    }
    public void ConnectToVoipServer()
    {
        GCVoip.OnPPTChange += GCVoip_OnPPTChange;
        GCVoip.OnJoinNewClient += GCVoip_OnJoinNewClient;
        GCVoip.OnConnectedToVoipServer += GCVoip_OnConnectedToVoipServer;
        GCVoip.OnClientDisconnected += GCVoip_OnClientDisconnected;
        GCVoip.Initialize(gameObject, roomName_field.text, displayName_field.text, Microphone.devices[0]);
    }

    private void GCVoip_OnClientDisconnected(VoipClientInfo info)
    {
        GameObject instance = GameObject.Find("vc" + info.clientId);
        if (instance != null)
            Destroy(instance);
    }

    public void OnPointerDown()
    {
        GCVoip.Recorder.PTTSwitch = true;
    }

    public void OnPointerUp()
    {
        GCVoip.Recorder.PTTSwitch = false;
    }

    public void Disconnect()
    {
        GCVoip.Disconnect();

        connected = false;
        ConnectionPanel.SetActive(true);
        RoomPanel.SetActive(false);

        for (int i = 0; i < Holder.transform.childCount; i++)
        {
            if (Holder.transform.GetChild(i) != null)
                Destroy(Holder.transform.GetChild(i).gameObject);
        }
    }

    private void GCVoip_OnJoinNewClient(VoipClientInfo info)
    {
        Debug.Log("So is Joined : " + info.displayName);

        GameObject instance = Instantiate(DisplayClient);
        instance.name = "vc" + info.clientId;
        instance.transform.parent = Holder.transform;

        instance.transform.localScale = new Vector3(1, 1, 1);
        instance.GetComponent<ClientDisplay>().SetDisplayName(info.displayName);
    }

    private void GCVoip_OnPPTChange(PTTStatus status, VoipClientInfo info)
    {
        GameObject displayClient = GameObject.Find("vc" + info.clientId);
        if (displayClient != null)
            displayClient.GetComponent<ClientDisplay>().ChangePTTStatus(status);
    }

    private void GCVoip_OnConnectedToVoipServer()
    {
        connected = true;
        Debug.Log("you joined to room -> " + roomName_field.text);

        ConnectionPanel.SetActive(false);
        RoomPanel.SetActive(true);
    }
}
