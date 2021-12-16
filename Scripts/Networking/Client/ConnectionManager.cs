using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AberrationGames.Networking.Client
{
    [RequireComponent(typeof(UnityClient))]
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug, "d_CacheServerConnected"), EditorTools.AberrationDescription("Server connection manager", "Jacob Cooper", "15/09/2021")]
    public class ConnectionManager : MonoBehaviour
    {
        public static ConnectionManager Instance;

        public UnityClient Client { get; private set; }

        public Shared.PlayerData playerData;

        public Dictionary<ushort, Shared.NetworkedPlayerLogic> connections;
        public Dictionary<ushort, Shared.PlayerData> dataConnections;

        public Shared.RoomData roomData { get; private set; }

        [EditorTools.AberrationButton(EditorTools.DeclaredButtonTypes.Button, false, "Connect", "Connect", 0)]

        [EditorTools.AberrationRequired] [SerializeField] private string _ipAdress;
        [SerializeField] private int _port;

        private int _desiredLobby = 0;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this);
        }

        public void Start()
        {
            connections = new Dictionary<ushort, Shared.NetworkedPlayerLogic>();
            Client = GetComponent<UnityClient>();

            Shared.NetworkInformation.Realm = Shared.NetworkRealm.Local;
            Shared.NetworkInformation.IsNetworking = true;

            dataConnections = new Dictionary<ushort, Shared.PlayerData>();

            StartCoroutine(WaitToConnect(0.1f, 0));
        }

        public void Initialize(string a_ipAdress, int a_port)
        {
            _ipAdress = a_ipAdress;
            _port = a_port;
        }

        public void Connect(int a_lobby)
        {
            _desiredLobby = a_lobby;
            Client.ConnectInBackground(IPAddress.Parse(_ipAdress), _port, true, ConnectCallback);
        }

        public void SetClientSelection(byte a_byte)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(a_byte);

                using (Message sendMessage = Message.Create((ushort)Shared.NetworkTags.LobbySelect, writer))
                {
                    Client.SendMessage(sendMessage, SendMode.Reliable);
                }
            }
        }

        IEnumerator WaitToConnect(float a_time, int a_connection)
        {
            yield return new WaitForSeconds(a_time);

            Client.MessageReceived += OnMessage;
            Connect(a_connection);
        }

        void OnDestroy()
        {
            //if (Client != null)
                //Client.MessageReceived -= OnMessage;
        }

        IEnumerator MountOntoPlayer(ushort a_id, bool a_local, byte a_playerID, Vector3 a_position, Vector3 a_rotation)
        {
            while (Base.Players.ChangingScenes)
            {
                yield return new WaitForSeconds(.1f);
            }

            yield return new WaitForSeconds(.1f);

            Debug.Log("Spawning player: " + a_id + ", is local = " + a_local);

            if (!connections.ContainsKey(a_playerID))
                Addressables.InstantiateAsync(Base.PlayerLoader.Instance.playerPrefab, a_position, Quaternion.Euler(a_rotation), null, true).Completed += handle =>
                {
                    GameObject result = handle.Result;

                    Shared.NetworkedPlayerLogic character = result.AddComponent<Shared.NetworkedPlayerLogic>();
                    character.Setup(a_playerID);

                    if (a_local)
                    {
                        Base.Players.PlayerDictionary[0] = new Base.PlayerData {
                            player = result,
                            playerID = a_playerID,
                            id = a_id,
                            input = Base.Players.PlayerDictionary[0].input,
                            device = Base.Players.PlayerDictionary[0].device
                        };
                    }
                    else
                    {
                        Destroy(result.GetComponent<Base.PlayerBase>());
                        Destroy(result.GetComponent<UnityEngine.InputSystem.PlayerInput>());
                    }
                    
                    connections.Add(a_id, character);
                };

            // Old way, might use

            //if (a_local)
            //{
            //    if (Base.Players.players.ContainsKey(0))
            //    {
            //        var ply = Base.Players.players[0];

            //        Shared.NetworkedPlayerLogic character = ply.player.AddComponent<Shared.NetworkedPlayerLogic>();
            //        character.Setup(a_playerID);
            //    }
            //}
            //else
            //{
            //    GameObject o = Instantiate(
            //        Base.Players.players[0].player.gameObject,
            //        a_position,
            //        Quaternion.Euler(a_rotation)
            //    );

            //    Shared.NetworkedPlayerLogic character = o.AddComponent<Shared.NetworkedPlayerLogic>();
            //    character.Setup(a_playerID);
            //    connections.Insert(a_id, character);
            //}

        }

        private void PlayerConnect(Message a_message)
        {
            Shared.PlayerData data = a_message.Deserialize<Shared.PlayerData>();

            // Local
            if (data.clientID == Client.ID && a_message.IsPingMessage)
            {
                data.lobby = (ushort)_desiredLobby;
                data.device = Base.Players.PlayerDictionary[0].device.displayName;

                playerData = data;

                using (Message sendMessage = Message.Create((ushort)Shared.NetworkTags.JoinLobby, data))
                {
                    sendMessage.MakePingAcknowledgementMessage(a_message);
                    Client.SendMessage(sendMessage, SendMode.Reliable);
                }
            }
        }

        private void SetMap(Shared.RoomData a_room)
        {
            //Debug.LogError("Changing Map to " + a_room.mapAddress);

            if (a_room.mapAddress != "Lobby")
                Shared.NetworkInformation.ShouldSpawnLocal = false;
            else
                Shared.NetworkInformation.ShouldSpawnLocal = true;

            Utils.ChangeScene.Instance.SetScene(a_room.mapAddress);
        }

        private void JoinLobby(Message a_message)
        {
            Debug.Log("Joining lobby");

            Shared.RoomData room = a_message.Deserialize<Shared.RoomData>();

            roomData = room;

            Debug.Log(roomData.roomID + " " + roomData.mapAddress);

            SetMap(room);
        }

        private void SetLobbyLeader()
        {
            foreach (var card in Base.PlayerLoader.Instance.canvas.GetComponentsInChildren<Events.PlayerCard>())
            {
                if (card.playerID == (int)roomData.masterPlayer)
                {
                    card.isMaster = true;
                }
                else
                    card.isMaster = false;
            }
        }

        private void ChangeLobbyData(Shared.RoomData a_roomData)
        {
            //Debug.LogError(a_roomData.mapAddress);

            if (a_roomData.mapAddress != roomData.mapAddress)
            {
                SetMap(a_roomData);

                roomData = a_roomData;

                return;
            }

            roomData = a_roomData;

            if (UpdateSelection.Instance != null)
            {
                if (roomData.roomInfo == 0)
                {
                    if (UpdateSelection.Instance.back != null)
                    {
                        GameObject obj = UpdateSelection.Instance.transform.parent.gameObject;
                        UpdateSelection.Instance.back.SetActive(true);
                        obj.SetActive(false);
                    }

                    SetLobbyLeader();
                }
                else if (roomData.roomInfo == 1)
                {
                    if (UpdateSelection.Instance.next != null)
                    {
                        GameObject obj = UpdateSelection.Instance.transform.parent.gameObject;
                        UpdateSelection.Instance.next.SetActive(true);
                        obj.SetActive(false);
                    }
                    else
                    {
                        UpdateSelection.Instance.UpdateVotes();
                    }
                }
            }
        }

        private void ChangeLobbyData(Message a_message)
        {
            Shared.RoomData room = a_message.Deserialize<Shared.RoomData>();

            ChangeLobbyData(room);
        }

        private void PlayerData(Message a_message)
        {
            Shared.PlayerData pdata = a_message.Deserialize<Shared.PlayerData>();

            if (pdata.clientID == Client.ID)
                playerData = pdata;

            // Make a leave version of this.
            if (roomData.mapAddress == "Lobby")
            {
                if (!dataConnections.ContainsKey(pdata.clientID))
                {
                    dataConnections[pdata.clientID] = pdata;

                    if (pdata.clientID == Client.ID)
                    {
                        Base.PlayerData data = new Base.PlayerData
                        {
                            id = pdata.playerID,
                            device = Base.Players.PlayerDictionary[0].device,
                            playerID = pdata.playerID,
                            input = Base.Players.PlayerDictionary[0].input,
                            player = Base.Players.PlayerDictionary[0].player
                        };

                        Base.Players.PlayerDictionary[0] = data;
                    }

                    Debug.Log($"Loading player: {pdata.playerID} | {pdata.device}");

                    // Changing player cards :brain:
                    foreach (var card in Base.PlayerLoader.Instance.canvas.GetComponentsInChildren<Events.PlayerCard>())
                    {
                        if (card.playerID == (int)pdata.playerID)
                        {
                            card.connected = true;
                            card.device = pdata.device;
                            break;
                        }
                    }
                }

                SetLobbyLeader();
            }
            else
            {
                //Debug.Log("Updating State for " + pdata.playerID + " from " + dataConnections[pdata.playerID].state.position + " to " + pdata.state.position);

                dataConnections[pdata.playerID] = pdata;
            }
        }

        private void LeaveLobby(Message a_message)
        {
            Shared.PlayerData pdata = a_message.Deserialize<Shared.PlayerData>();

            //Debug.LogError($"Player {pdata.clientID} has left the lobby.");

            if (pdata.clientID != Client.ID)
                foreach (var card in Base.PlayerLoader.Instance.canvas.GetComponentsInChildren<Events.PlayerCard>())
                {
                    if (card.playerID == (int)pdata.playerID)
                    {
                        card.connected = false;
                        card.device = "";
                        break;
                    }
                }
        }

        private void OnMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                switch ((Shared.NetworkTags)message.Tag)
                {
                    case Shared.NetworkTags.PlayerConnect:

                        PlayerConnect(message);

                        break;
                    case Shared.NetworkTags.JoinLobby:

                        JoinLobby(message);

                        break;
                    case Shared.NetworkTags.LeaveLobby:

                        LeaveLobby(message);

                        break;
                    case Shared.NetworkTags.PlayerDisconnect:

                        //PlayerDisconnect(message);

                        //Destroy(this);
                        break;
                    case Shared.NetworkTags.ChangeLobbyInfo:

                        ChangeLobbyData(message);

                        break;
                    case Shared.NetworkTags.Data: // Player data.

                        PlayerData(message);

                        break;

                    case Shared.NetworkTags.PlayerSpawn: // Spawn another player.

                        Shared.PlayerData pdata = message.Deserialize<Shared.PlayerData>();

                        dataConnections[pdata.clientID] = pdata;

                        StartCoroutine(MountOntoPlayer(pdata.clientID, pdata.clientID == Client.ID, pdata.playerID, pdata.state.position, pdata.state.rotation));

                        break;

                    case Shared.NetworkTags.Event:
                        using (DarkRiftReader reader = message.GetReader())
                        {
                            byte gameEvent = reader.ReadByte();                  
                        }

                        break;
                }
            }
        }

        private void ConnectCallback(Exception exception)
        {
            if (Client.ConnectionState == ConnectionState.Connected)
            {
                //Debug.LogError("Connected to server");
            }
            else
            {
                //Debug.LogError("Unable to connect to server.");
            }
        }
    }
}
