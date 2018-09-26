﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Newtonsoft.Json;
using System.IO;

namespace ARIIVC.Logger
{
    public class DrivePackDetailsAndCredentialsStore
    {
        internal PackDetailsUserCredentialsData PackDetailsUserCredentialsDataObject;
        private static volatile DrivePackDetailsAndCredentialsStore _instance;
        private static readonly object SyncRoot = new Object();
        private DrivePackDetailsAndCredentialsStore()
        {
            string textInFile = File.ReadAllText("testdata\\DrivePackDetailsAndCredentialsStore.json");
            PackDetailsUserCredentialsDataObject = JsonConvert.DeserializeObject<PackDetailsUserCredentialsData>(
                        textInFile);
        }

        private static DrivePackDetailsAndCredentialsStore Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new DrivePackDetailsAndCredentialsStore();
                }
                return _instance;
            }
        }

        private static string GetDefaultPassword(string username)
        {
            var password = string.Empty;
            var defaultCred = Instance.PackDetailsUserCredentialsDataObject.DefaultUserCredentials.Find(t => t.Username == username);
            if (defaultCred != null)
                password = defaultCred.Password;
            return password;
        }

        private static PackDetailsData GetPackDetailsDataFromServerHostName(string server, string service)
        {
            var packDetail = Instance.PackDetailsUserCredentialsDataObject.PackDetails.Find
            (t => String.Equals(t.Server, server, StringComparison.InvariantCultureIgnoreCase) &&
                  String.Equals(t.Service, service, StringComparison.InvariantCultureIgnoreCase));
            return packDetail;
        }

        private static PackDetailsData GetPackDetailsDataFromServerFriendlyName(string packFriendlyName)
        {
            packFriendlyName = packFriendlyName.Replace(" ", string.Empty);
            packFriendlyName = packFriendlyName.Replace("-", string.Empty);
            var packDetail = Instance.PackDetailsUserCredentialsDataObject.PackDetails.Find
                (t => String.Equals(t.FriendlyName, packFriendlyName, StringComparison.InvariantCultureIgnoreCase));
            return packDetail;
        }

        public static string GetPassword(string server, string service, string username)
        {
            var password = GetDefaultPassword(username);
            var packDetail = GetPackDetailsDataFromServerHostName(server, service);
            if (packDetail.OverriddenUserCredentials != null)
            {
                var credential = packDetail.OverriddenUserCredentials.Find(t => t.Username == username);
                if (credential != null)
                    password = credential.Password;
            }
            return password;
        }

        public static string GetPassword(string packFriendlyName, string username)
        {
            var password = GetDefaultPassword(username);
            var packDetail = GetPackDetailsDataFromServerFriendlyName(packFriendlyName);
            if (packDetail.OverriddenUserCredentials != null)
            {
                var credential = packDetail.OverriddenUserCredentials.Find(t => t.Username == username);
                if (credential != null)
                    password = credential.Password;
            }
            return password;
        }

        public static string GetPassword(string username)
        {
            string server = ConfigurationManager.AppSettings["server"] ?? "";
            string service = ConfigurationManager.AppSettings["service"] ?? "";
            return GetPassword(server, service, username);
        }




    }


    class PackDetailsUserCredentialsData
    {
        [JsonProperty("usercredentials.default")]
        public List<UserCredentialsData> DefaultUserCredentials;
        [JsonProperty("packdetails")]
        public List<PackDetailsData> PackDetails;
    }


    class UserCredentialsData
    {
        [JsonProperty("username")]
        public string Username;
        [JsonProperty("password")]
        public string Password;

    }


    class PackDetailsData
    {
        [JsonProperty("server")]
        public string Server;

        [JsonProperty("service")]
        public string Service;

        [JsonProperty("friendlyname")]
        public string FriendlyName;

        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>
            Properties;

        [JsonProperty("usercredentials.override", NullValueHandling = NullValueHandling.Ignore)]
        public
            List<UserCredentialsData> OverriddenUserCredentials;
    }


}