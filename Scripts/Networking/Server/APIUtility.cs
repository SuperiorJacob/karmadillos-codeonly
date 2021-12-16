using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System;

namespace AberrationGames.Networking.Server
{
    public sealed class APIUtility
    {
        public static APIUtility Instance;

        private static readonly string authenticationKey = "&ew!mF81!Te^H*%@^*k%4K*$XCsJt";
        private static readonly string postServer = "https://api.aberrationgames.net/api/v1/server/list-server/";
        private static readonly string postLobby = "https://api.aberrationgames.net/api/v1/server/create-lobby/";

        public static APIUtility Create()
        {
            APIUtility util = new APIUtility();

            Instance = util;

            return util;
        }

        public void AddServer(string a_server, string a_port)
        {
            if (Shared.NetworkInformation.Realm != Shared.NetworkRealm.Server) return;

            string json = "{\"ip_address\": \"" + a_server + "\", \"port\": \"" + a_port + "\"}";

            ServerManager.Instance.StartCoroutine(Post(postServer, json));
        }

        public void RemoveServer(int a_serverID)
        {
            if (Shared.NetworkInformation.Realm != Shared.NetworkRealm.Server) return;
        }

        public void AddLobby(string a_name, string a_id, string a_playerCount, string a_maxPlayers, string a_serverIP, string a_mapName)
        {
            if (Shared.NetworkInformation.Realm != Shared.NetworkRealm.Server) return;

            string json = "{\"name\": \"" + a_name + "\", \"lobby_id\": " + a_id + ", \"player_count\": " + a_playerCount + ", \"max_players\": " + a_maxPlayers + ", \"server_ip\": \"" + a_serverIP + "\", \"map_name\": " + a_mapName + "}";

            ServerManager.Instance.StartCoroutine(Post(postServer, json));
        }

        public void RemoveLobby(int a_lobbyNumber)
        {
            if (Shared.NetworkInformation.Realm != Shared.NetworkRealm.Server) return;
        }

        private IEnumerator Post(string a_uri, string a_message)
        {
            UnityWebRequest serverListRequest = UnityWebRequest.Post(a_uri, a_message);
            serverListRequest.SetRequestHeader("Authorization", authenticationKey);
            serverListRequest.SetRequestHeader("Content-Type", "application/json");

            yield return serverListRequest.SendWebRequest();

            //if (serverListRequest.result == UnityWebRequest.Result.ConnectionError ||
            //    serverListRequest.result == UnityWebRequest.Result.ProtocolError)
            //{
            //    Debug.LogError(serverListRequest.error);
            //    yield break;
            //}

            //JSONNode serverInfo = JSON.Parse(serverListRequest.downloadHandler.text);
        }

        public static string GetIPAddress()
        {
            String address = "";
            WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
            using (WebResponse response = request.GetResponse())
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = stream.ReadToEnd();
            }

            int first = address.IndexOf("Address: ") + 9;
            int last = address.LastIndexOf("</body>");
            address = address.Substring(first, last - first);

            return address;
        }
    }
}
