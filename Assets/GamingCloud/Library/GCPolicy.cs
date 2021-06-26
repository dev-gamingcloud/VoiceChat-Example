using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace gamingCloud
{
    public class GCPolicy : GCEncryption
    {
        public enum QueryMode
        {
            HTTP, Socket, TCP
        }

        public static string UniqueId
        {
            get
            {
                return SystemInfo.deviceUniqueIdentifier.ToString();
            }
        }

        public static string playerToken
        {
            get { return PlayerPrefs.HasKey("gc-prefs-token") ? PlayerPrefs.GetString("gc-prefs-token") : null; }
            set { return; }
        }

        public static string _createAcceptKey()
        {
            return createAcceptKey();
        }

        public static Dictionary<string, string> GetRequiredQueries(QueryMode mode)
        {
            Dictionary<string, string> RequiredQueries = new Dictionary<string, string>();

            if (mode == QueryMode.TCP)
                RequiredQueries.Add("acceptKey", createAcceptKey());

            else
                RequiredQueries.Add("ak", createAcceptKey());

            if (playerToken != null)
            {
                if (mode == QueryMode.HTTP)
                {
                    RequiredQueries.Add("token", playerToken);
                }
                else if (mode == QueryMode.Socket)
                {
                    RequiredQueries.Add("utoken", playerToken);

                }
            }
            if (mode == QueryMode.TCP)
            {
                RequiredQueries.Add("IsEditor", Application.isEditor.ToString());
                RequiredQueries.Add("packageName", Application.identifier.ToString());
                RequiredQueries.Add("DeviceId", UniqueId);

            }
            else
            {
                RequiredQueries.Add("IsEditor", Application.isEditor.ToString());
                RequiredQueries.Add("GameVersion", Application.version.ToString());
                RequiredQueries.Add("devicetype", SystemInfo.deviceType.ToString());
                RequiredQueries.Add("UnityVersion", Application.unityVersion.ToString());
                RequiredQueries.Add("packageName", Application.identifier.ToString());
                RequiredQueries.Add("DeviceId", UniqueId);
                RequiredQueries.Add("os", SystemInfo.operatingSystem.ToString());
                RequiredQueries.Add("platfromName", Application.platform.ToString());
            }
            if (mode == QueryMode.HTTP)
            {
                RequiredQueries.Add("gameToken", Config.ServiceToken);
            }
            else if (mode == QueryMode.TCP)
            {
                RequiredQueries.Add("gtoken", Config.ServiceToken);
            }
            else if (mode == QueryMode.Socket)
            {
                RequiredQueries.Add("gtoken", Config.ServiceToken);
                RequiredQueries.Add("player_type", "v2");

            }
            return RequiredQueries;
        }
        public static string Md5Generator(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);
            string hashString = "";
            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }
            return hashString.PadLeft(32, '0');
        }



         public static long getTimeStamp()
        {
            return getTimeStamp();
        }
    }
}
