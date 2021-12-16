using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using UnityEngine;

namespace AberrationGames.Networking.Server
{
    [RequireComponent(typeof(XmlUnityServer))]
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug, "d_CacheServerDisconnected")]
    public class ServerManager : MonoBehaviour
    {
        public static ServerManager Instance;

        public Dictionary<ushort, ClientConnection> players;

        private XmlUnityServer _xmlServer;
        private DarkRiftServer _server;

        public ClientConnection GetClient(ushort a_clientID)
        {
            return players[a_clientID];
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);

            Shared.NetworkInformation.Realm = Shared.NetworkRealm.Server;
            Shared.NetworkInformation.IsNetworking = true;

            APIUtility util = APIUtility.Create();

            string publicIP = APIUtility.GetIPAddress();

            util.AddServer(publicIP, "4296");
        }

        void Start()
        {
            players = new Dictionary<ushort, ClientConnection>();

            _xmlServer = GetComponent<XmlUnityServer>();
            _server = _xmlServer.Server;

            _server.ClientManager.ClientConnected += OnClientConnected;
            _server.ClientManager.ClientDisconnected += OnClientDisconnected;
        }

        void OnDestroy()
        {
            _server.ClientManager.ClientConnected -= OnClientConnected;
            _server.ClientManager.ClientDisconnected -= OnClientDisconnected;
        }

        private void OnClientDisconnected(object a_sender, ClientDisconnectedEventArgs a_event)
        {
            IClient client = a_event.Client;

            if (players.TryGetValue(client.ID, out ClientConnection connection))
            {
                connection.OnClientDisconnect(a_sender, a_event);
            }

            a_event.Client.MessageReceived -= OnMessage;
        }

        private void OnClientConnected(object a_sender, ClientConnectedEventArgs a_event)
        {
            IClient client = a_event.Client;

            a_event.Client.MessageReceived += OnMessage;

            using (Message message = Message.Create((ushort)Shared.NetworkTags.PlayerConnect, new Shared.PlayerData(client.ID, 0, 0)))
            {
                message.MakePingMessage();
                client.SendMessage(message, SendMode.Reliable);
            }
        }

        private void OnMessage(object a_sender, MessageReceivedEventArgs a_event)
        {
            using (Message message = a_event.GetMessage())
            {
                switch ((Shared.NetworkTags)message.Tag)
                {
                    case Shared.NetworkTags.JoinLobby:
                        OnClientConnect(a_event.Client, message.Deserialize<Shared.PlayerData>());
                        break;
                }
            }
        }

        private void OnClientConnect(IClient a_client, Shared.PlayerData a_player)
        {
            Debug.LogError($"Player {a_client.ID} has connected to lobby {a_player.lobby}, ping: {(a_client.RoundTripTime.LatestRtt / 2) * 1000}ms");

            // Setups the connection.
            a_client.MessageReceived -= OnMessage;

            new ClientConnection(a_client, a_player);
        }
    }
}
