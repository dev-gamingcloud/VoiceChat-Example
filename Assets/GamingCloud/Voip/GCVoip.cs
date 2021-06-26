using gamingCloud.Network.tcp;
using gamingCloud.Network.udp;
using gamingCloud.Network.Voip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace gamingCloud.Network.Voip
{
    public enum PTTStatus
    {
        Enabled = 1,
        Disabled = 0,
    }
    public class GCVoip
    {

        #region schemas
        class joinSchema
        {
            public int vq;
            public string pid, cid, name;
        }

        class ChangeChannelSchema
        {
            public string channel;
            public string issue = "changeChannel";
        }

        #endregion

        #region fields
        static string _roomName, _displayName;
        static bool connectedToUdp = false;


        static bool OnceCalled = false;

        static tcpStreamer tcpStreamer;

        static udpStreamer udpStreamer;

        static string address = "voip.gcsc.ir";
        static int port = 10012;

        static string _clientId;

        public string clientId
        {
            get { return _clientId; }
        }

        static int Step = 25;
        static VoiceRecorder recorder;
        public static VoiceRecorder Recorder
        {
            get
            {
                return recorder;
            }
        }
        static GameObject currentInstance;

        private static byte _currnetChannel = (byte)GCChannels.Public;

        public static GCChannels CurrentChannel
        {
            get { return (GCChannels)_currnetChannel; }
        }

        #endregion

        #region Actions
        public static event Action OnConnectedToVoipServer;

        public static event Action<VoiceQuality, VoipClientInfo> OnFrequencyChange;
        public static event Action<PTTStatus, VoipClientInfo> OnPPTChange;
        public static event Action<VoipClientInfo> OnJoinNewClient, OnClientDisconnected, OnChannelChanged;
        #endregion

        #region Private Methods
        private static void ConnectToUdpServer()
        {
            if (connectedToUdp)
                return;

            udpStreamer.sendPacket("con/" + _clientId);

            Task.Delay(2 * 1000).ContinueWith((t) => ConnectToUdpServer());
        }

        private static void OnUdpRecvPacket(byte[] packet)
        {

            if (packet.Length == 3)
                connectedToUdp = true;
            else
            {
                string cid = Encoding.UTF8.GetString(packet.Skip(0).Take(6).ToArray());

                GameObject client = GameObject.Find("voip/" + cid);
                if (client != null)
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        if (client.GetComponent<VoiceSpeaker>() != null)
                            client.GetComponent<VoiceSpeaker>().PlayClip(packet.Skip(6).ToArray());
                    });
                }
            }

        }

        private static void OnUdpReady()
        {
            ConnectToUdpServer();
        }

        private static void OnTcpRecvPacket(string data)
        {

            string[] splitedPackets = data.Split(new string[] { "$$" }, StringSplitOptions.None);

            foreach (string _splitedPacket in splitedPackets)
            {
                string splitedPacket = _splitedPacket;
                splitedPacket = splitedPacket.Trim();
                if (splitedPacket == "")
                    continue;

                if (splitedPacket == "ping")
                {
                    tcpStreamer.sendPacket("pong");
                    continue;
                }

                JObject packet = JObject.Parse(splitedPacket);

                switch (packet["issue"].ToString())
                {
                    case "cid":

                        _clientId = packet["cid"].ToObject<string>();

                        udpStreamer = new udpStreamer();
                        udpStreamer.OnReady += OnUdpReady;
                        udpStreamer.OnRecvPacket += OnUdpRecvPacket;

                        udpStreamer.Connect(address, port);
                        break;

                    case "init":
                        
                        var clients = packet["clients"].ToObject<joinSchema[]>();
                        foreach (var item in clients)
                        {
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                GameObject obj = initVoipClient(item.cid, item.pid, item.name, item.vq);
                                if (OnJoinNewClient != null)
                                    OnJoinNewClient(obj.GetComponent<VoipClientInfo>());

                            });

                        }

                        if (OnConnectedToVoipServer != null)
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                recorder.StartMicrophone();
                                OnConnectedToVoipServer();
                            });

                        break;

                    case "join":
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            joinSchema join = JsonConvert.DeserializeObject<joinSchema>(splitedPacket);
                            GameObject obj = initVoipClient(join.cid, join.pid, join.name, join.vq);

                            if (OnJoinNewClient != null)
                                OnJoinNewClient(obj.GetComponent<VoipClientInfo>());
                        });
                        break;

                    case "dc":
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {

                            GameObject client = GetVoipClient(packet["cid"].ToString());
                            if (client != null)
                            {
                                GameObject.Destroy(client);
                                if (OnClientDisconnected != null)
                                    OnClientDisconnected(client.GetComponent<VoipClientInfo>());
                            }

                        });
                        break;

                    case "ptt":
                        bool status = packet["state"].ToObject<bool>();
                        string id = packet["cid"].ToString();

                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            GameObject go = GetVoipClient(id);

                            if (go != null)
                            {
                                if (OnPPTChange != null)
                                    OnPPTChange(status ? PTTStatus.Enabled : PTTStatus.Disabled, go.GetComponent<VoipClientInfo>());

                                if (status == true)
                                    go.GetComponent<VoiceSpeaker>().ClearQueue();
                            }

                        });
                        break;

                    case "changeVq":
                        string id2 = packet["cid"].ToString();
                        int vq = packet["vq"].ToObject<int>();
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            GameObject go = GetVoipClient(id2);

                            if (go != null)
                            {
                                VoipClientInfo vci = go.GetComponent<VoipClientInfo>();
                                vci.VoiceQuality = vq;
                                if (OnFrequencyChange != null)
                                    OnFrequencyChange((VoiceQuality)vq, vci);
                            }

                        });
                        break;

                    case "changeChannel":
                        string id3 = packet["cid"].ToString();
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            GameObject go = GetVoipClient(id3);

                            if (go != null)
                            {
                                if (OnChannelChanged != null)
                                    OnChannelChanged(go.GetComponent<VoipClientInfo>());
                            }

                        });
                        break;


                }
            }

        }

        private static void tcpStreamerConnected()
        {

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Dictionary<string, dynamic> auth = new Dictionary<string, dynamic>()
                    {
                        { "issue" , "auth" },
                        { "ak" ,  GCPolicy._createAcceptKey()},
                        { "gtk" , GCPolicy.Config.ServiceToken },
                        { "pid" , GCPolicy.playerToken},
                        { "uid" , GCPolicy.UniqueId},
                        { "vq" , recorder.FrequencyRate },
                        { "name" , _displayName },
                        { "room" , _roomName },
                        { "isEditor" , Application.isEditor }
                    };

                tcpStreamer.sendPacket(JsonConvert.SerializeObject(auth));
            });
        }

        private static void Recorder_OnSendPacket(byte[] data)
        {
            udpStreamer.sendPacket(data, _clientId);
        }

        private static void Recorder_OnPTTSendPacket(string data)
        {
            Task.Delay(Step * 10).ContinueWith((t) => tcpStreamer.sendPacket(data));
        }


        static GameObject initVoipClient(string cid, string PlayerID, string dname, int voiceQuality)
        {

            GameObject instance = new GameObject("voip/" + cid + "");
            VoipClientInfo info = instance.AddComponent<VoipClientInfo>();
            info.clientId = cid;
            info.displayName = dname;
            info.playerId = PlayerID;
            info.step = Step;
            info.VoiceQuality = voiceQuality;

            instance.AddComponent<VoiceSpeaker>();

            return instance;
        }

        #endregion

        #region Authenticate Overloads
        static VoiceRecorder Authenticate(string roomName, string displayName, string microphone, GameObject gameObject, VoiceQuality voiceQuality)
        {
            currentInstance = gameObject;

            if (currentInstance.GetComponent<UnityMainThreadDispatcher>() == null)
                currentInstance.AddComponent<UnityMainThreadDispatcher>();

            if (currentInstance.GetComponent<VoipClientDisconnection>() == null)
                currentInstance.AddComponent<VoipClientDisconnection>();

            if (currentInstance.GetComponent<AudioSource>() == null)
                currentInstance.AddComponent<AudioSource>();

            if (currentInstance.GetComponent<VoiceRecorder>() == null)
                recorder = currentInstance.AddComponent<VoiceRecorder>();

            recorder.OnSendPacket += Recorder_OnSendPacket;
            recorder.OnPTTSendPacket += Recorder_OnPTTSendPacket;

            _roomName = roomName;
            _displayName = displayName;
            recorder.microphone = microphone;
            recorder.FrequencyRate = (int)voiceQuality;


            tcpStreamer = new tcpStreamer(address, port, 10 * 1024);
            tcpStreamer.OnConnected += tcpStreamerConnected;
            tcpStreamer.OnPacketRecieve += OnTcpRecvPacket;

            return recorder;
        }
        static VoiceRecorder Authenticate(string roomName, string displayName, string microphone, GameObject gameObject)
        {

            currentInstance = gameObject;
            if (currentInstance.GetComponent<UnityMainThreadDispatcher>() == null)
                currentInstance.AddComponent<UnityMainThreadDispatcher>();

            if (currentInstance.GetComponent<VoipClientDisconnection>() == null)
                currentInstance.AddComponent<VoipClientDisconnection>();

            if (currentInstance.GetComponent<AudioSource>() == null)
                currentInstance.AddComponent<AudioSource>();

            if (currentInstance.GetComponent<VoiceRecorder>() == null)
                recorder = currentInstance.AddComponent<VoiceRecorder>();

            recorder.OnSendPacket += Recorder_OnSendPacket;
            recorder.OnPTTSendPacket += Recorder_OnPTTSendPacket;

            _roomName = roomName;
            _displayName = displayName;
            recorder.microphone = microphone;
            recorder.FrequencyRate = (int)VoiceQuality.Medium;


            tcpStreamer = new tcpStreamer(address, port, 10 * 1024);
            tcpStreamer.OnConnected += tcpStreamerConnected;
            tcpStreamer.OnPacketRecieve += OnTcpRecvPacket;


            return recorder;

        }
        #endregion

        #region Initialize Overloads
        public static void Initialize(GameObject gameobject, string RoomName, string DisplayName, string Microphone, KeyCode PTTKey, VoiceQuality voiceQuality)
        {
            if (OnceCalled == false)
            {
                OnceCalled = true;
                if (RoomName == String.Empty)
                {
                    Debug.LogError("There is No Room Name. ");
                    return;
                }
                if (DisplayName == String.Empty)
                {
                    Debug.LogError("There is No Display Name. ");
                    return;
                }
                if (Microphone == String.Empty)
                {
                    Debug.LogError("Please Adjust Microphone. ");
                    return;
                }
                else
                {

                    VoiceRecorder recorder = Authenticate(RoomName, DisplayName, Microphone, gameobject, voiceQuality);
                    recorder.PTTKeyBtn = PTTKey;
                    recorder.IsPTTSwitch = false;

                }
            }
            else
                Debug.LogError("This Method Can be Called Once");

        }
        public static void Initialize(GameObject gameobject, string RoomName, string DisplayName, string Microphone, KeyCode PTTKey, VoiceQuality voiceQuality, int delay)
        {
            if (OnceCalled == false)
            {
                OnceCalled = true;
                if (RoomName == String.Empty)
                {
                    Debug.LogError("There is No Room Name. ");
                    return;
                }
                if (DisplayName == String.Empty)
                {
                    Debug.LogError("There is No Display Name. ");
                    return;
                }
                if (Microphone == String.Empty)
                {
                    Debug.LogError("Please Adjust Microphone. ");
                    return;
                }
                else
                {
                    Step = delay;

                    VoiceRecorder recorder = Authenticate(RoomName, DisplayName, Microphone, gameobject, voiceQuality);
                    recorder.PTTKeyBtn = PTTKey;
                    recorder.IsPTTSwitch = false;
                }
            }
            else
                Debug.LogError("This Method Can be Called Once");
        }
        public static void Initialize(GameObject gameObject, string RoomName, string DisplayName, string microphone, VoiceQuality voiceQuality, int delay)
        {
            if (OnceCalled == false)
            {
                OnceCalled = true;
                if (RoomName == String.Empty)
                {
                    Debug.LogError("There is No Room Name. ");
                    return;
                }
                if (DisplayName == String.Empty)
                {
                    Debug.LogError("There is No Display Name. ");
                    return;
                }
                if (microphone == String.Empty)
                {
                    Debug.LogError("Please Adjust Microphone. ");
                    return;
                }
                else
                {
                    Step = delay;

                    Authenticate(RoomName, DisplayName, microphone, gameObject, voiceQuality);
                }
            }
            else
                Debug.LogError("This Method Can be Called Once");

        }
        public static void Initialize(GameObject gameObject, string RoomName, string DisplayName, string microphone, VoiceQuality voiceQuality)
        {
            if (OnceCalled == false)
            {
                OnceCalled = true;
                if (RoomName == String.Empty)
                {
                    Debug.LogError("There is No Room Name. ");
                    return;
                }
                if (DisplayName == String.Empty)
                {
                    Debug.LogError("There is No Display Name. ");
                    return;
                }
                if (microphone == String.Empty)
                {
                    Debug.LogError("Please Adjust Microphone. ");
                    return;
                }
                else
                {
                    Authenticate(RoomName, DisplayName, microphone, gameObject, voiceQuality);
                }
            }
            else
                Debug.LogError("This Method Can be Called Once");
        }
        public static void Initialize(GameObject gameObject, string RoomName, string DisplayName, string microphone)
        {
            if (OnceCalled == false)
            {

                OnceCalled = true;
                if (RoomName == String.Empty)
                {
                    Debug.LogError("There is No Room Name. ");
                    return;
                }
                if (DisplayName == String.Empty)
                {
                    Debug.LogError("There is No Display Name. ");
                    return;
                }
                if (microphone == String.Empty)
                {
                    Debug.LogError("Please Adjust Microphone. ");
                    return;
                }
                else
                {

                    Authenticate(RoomName, DisplayName, microphone, gameObject);
                }
            }
            else
                Debug.LogError("This Method Can be Called Once");
        }
        #endregion

        #region Public Methods
        public static void ChangeMicrophone(string newMicrophone)
        {
            Microphone.End(recorder.microphone);
            recorder.microphone = newMicrophone;
            recorder.StartMicrophone();
        }

        public static void ChangeFrequency(VoiceQuality voiceQuality)
        {
            recorder.FrequencyRate = (int)voiceQuality;
            Microphone.End(recorder.microphone);
            recorder.StartMicrophone();

            tcpStreamer.sendPacket(JsonConvert.SerializeObject(new Dictionary<string, dynamic>() {

                {"issue" ,"changeVq" },
                {"vq" ,(int)voiceQuality}
            }));

        }

        public static void Disconnect()
        {
            OnceCalled = false;
            connectedToUdp = false;
            if (udpStreamer != null)
            {
                udpStreamer.Disconnect();
                udpStreamer.OnReady -= OnUdpReady;
                udpStreamer.OnRecvPacket -= OnUdpRecvPacket;

            }

            try
            {
                tcpStreamer.OnConnected -= tcpStreamerConnected;
                tcpStreamer.OnPacketRecieve -= OnTcpRecvPacket;

                if (tcpStreamer != null)
                {
                    tcpStreamer.sendPacket("dc");
                    tcpStreamer.Disconnect();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                if (tcpStreamer != null)
                {
                    tcpStreamer.Disconnect();
                }

            }

            VoipClientInfo[] clients = GameObject.FindObjectsOfType<VoipClientInfo>();
            foreach (VoipClientInfo item in clients)
                GameObject.Destroy(item.instance);

            if (currentInstance != null)
            {
                if (currentInstance.GetComponent<VoiceRecorder>() != null)
                {
                    recorder.OnSendPacket -= Recorder_OnSendPacket;
                    recorder.OnPTTSendPacket -= Recorder_OnPTTSendPacket;

                    GameObject.Destroy(currentInstance.GetComponent<VoiceRecorder>());
                }

                if (currentInstance.GetComponent<AudioSource>() != null)
                    GameObject.Destroy(currentInstance.GetComponent<AudioSource>());

                if (currentInstance.GetComponent<VoipClientDisconnection>() != null)
                    GameObject.Destroy(currentInstance.GetComponent<VoipClientDisconnection>());
            }

            OnClientDisconnected = null;
            OnConnectedToVoipServer = null;
            OnFrequencyChange = null;
            OnJoinNewClient = null;
            OnJoinNewClient = null;
        }

        public static void ChangeChannel(GCChannels channel)
        {

            if (channel == CurrentChannel)
                return;

            _currnetChannel = (byte)channel;

            string data = JsonUtility.ToJson(new ChangeChannelSchema()
            {
                channel = "" + _currnetChannel
            });


            tcpStreamer.sendPacket(data);
        }

        public static GameObject GetVoipClient(VoipClientInfo info)
        {
            return GetVoipClient(info.clientId);
        }
        public static GameObject GetVoipClient(string id)
        {
            GameObject go = GameObject.Find("voip/" + id);

            return go;
        }

        public static bool ItsMyClient(VoipClientInfo info)
        {
            return ItsMyClient(info.clientId);
        }
        public static bool ItsMyClient(string id)
        {
            return id == _clientId;
        }
        #endregion


    }

}
