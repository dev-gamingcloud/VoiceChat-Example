using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace gamingCloud.Network.udp
{
    public class udpStreamer
    {

        static UdpClient client;

        int _clientPort;
        public int clientPort
        {
            get
            {
                return _clientPort;
            }
        }

        public object lockToken = new object();

        public event Action<byte[]> OnRecvPacket;
        public event Action OnReady;

        public void Connect(string address, int port)
        {
            IPEndPoint iPEndPoint;
            System.Random rnd = new System.Random();
            _clientPort = rnd.Next(10000, 20000);

            client = new UdpClient(_clientPort);
            iPEndPoint = new IPEndPoint(IPAddress.Any, port);
            client.Connect(address, port);

            try
            {

                Task.Run(() =>
                {
                    if (OnReady != null)
                        UnityMainThreadDispatcher.Instance().Enqueue(() => OnReady());
                    while (true)
                    {
                        byte[] recv = client.Receive(ref iPEndPoint);

                        if (OnRecvPacket != null)
                            UnityMainThreadDispatcher.Instance().Enqueue(() => OnRecvPacket(recv));
                    }

                });

            }
            catch (Exception e)
            {
                Debug.LogError(e);

            }
        }

        static void _sendPacket(byte[] packet)
        {
            int maxPacketSize = 65500;
            if (packet.Length <= maxPacketSize)
            {
                client.Send(packet, packet.Length);


            }
            else
            {
                for (int i = 0; i < packet.Length; i += maxPacketSize)
                    _sendPacket(packet.Skip(i).Take(maxPacketSize).ToArray());
            }
        }
        void _sendPacket(byte[] packet, string cid)
        {
            //Debug.Log("len: "+ Encoding.ASCII.GetBytes(cid).Length);
            byte[] cb = Encoding.ASCII.GetBytes(cid);
            int maxPacketSize = 65500 - cb.Length;

            if (packet.Length <= maxPacketSize - cb.Length)
            {
                List<byte> tmp = packet.ToList();

                tmp.InsertRange(0, cb);
                client.Send(tmp.ToArray(), packet.Length);
            }
            else
            {
                for (int i = 0; i < packet.Length; i += maxPacketSize - cb.Length)
                {
                    _sendPacket(packet);
                }
            }
        }

        public void sendPacket(string packet)
        {
            byte[] _packet = Encoding.ASCII.GetBytes(packet);
            _sendPacket(_packet);
        }

        public void sendPacket(byte[] packet, string cid)
        {
            _sendPacket(packet, cid);

        }

        public void sendPacket(byte[] packet)
        {
            _sendPacket(packet);
        }

        public void Disconnect()
        {
            //if (Application.isEditor)
            lock (lockToken)
            {
                if (client != null)
                    client.Dispose();
            }
            //else
            //{
            if (client != null)
                client.Dispose();
            //}
        }

    }

}