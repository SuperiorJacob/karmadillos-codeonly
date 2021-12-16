using System;
using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AberrationGames.Networking.Server
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug), EditorTools.AberrationDescription("Server sided room behaviour for multiple simulations (lobbies).", "Jacob Cooper", "15/09/2021")]
    public class Room : MonoBehaviour
    {
        public Shared.RoomData roomData;

        public List<ClientConnection> clientConnections = new List<ClientConnection>();
        public Dictionary<ushort, GameObject> clientObjects = new Dictionary<ushort, GameObject>();

        public Dictionary<ushort, GameObject> networkedGameObjects = new Dictionary<ushort, GameObject>();

        public PhysicsScene physicsScene { get; private set; }

        private Scene _scene;

        private float _waitTime = 0;

        public void RelayClient(ClientConnection a_connection)
        {
            foreach (var client in clientConnections)
            {
                //if (a_connection == client)
                //    continue;

                // Tell the player all about the other players
                using (Message message = Message.Create((ushort)Shared.NetworkTags.Data, client.playerData))
                {
                    a_connection.client.SendMessage(message, SendMode.Reliable);
                }

                // Tell every client this player joining
                using (Message message = Message.Create((ushort)Shared.NetworkTags.Data, a_connection.playerData))
                {
                    client.client.SendMessage(message, SendMode.Reliable);
                }
            }

            using (Message message = Message.Create((ushort)Shared.NetworkTags.ChangeLobbyInfo, roomData))
            {
                a_connection.client.SendMessage(message, SendMode.Reliable);
            }
        }

        public void MoveObjectTo(GameObject a_object)
        {
            SceneManager.MoveGameObjectToScene(a_object, _scene);
        }

        private void LobbyResetInfo()
        {
            for (int i = 0; i < roomData.slots; i++)
            {
                roomData.playerInfo[i] = 0;
            }
        }

        public void LobbySelect(ClientConnection a_client, Message a_message)
        {
            if (roomData.mapAddress == "Lobby")
            {
                using (DarkRiftReader reader = a_message.GetReader())
                {
                     roomData.playerInfo[a_client.playerData.playerID] = reader.ReadByte();
                }

                if (roomData.roomInfo == 0 && roomData.slots > 1)
                {
                    bool shouldChange = true;
                    for (int i = 0; i < roomData.slots; i++)
                    {
                        var correct = roomData.playerInfo[i];

                        if (correct != 1)
                            shouldChange = false;
                    }

                    Debug.LogError(roomData.slots + " " + shouldChange);

                    if (shouldChange)
                    {
                        roomData.roomInfo = 1;

                        LobbyResetInfo();

                        // Waits 15s before picking a map, later on dont be lazy and tell the client.
                        _waitTime = Time.realtimeSinceStartup + Shared.NetworkInformation.WaitTime;

                        Debug.LogError(_waitTime);
                    }
                }
                
                if (roomData.roomInfo == 1)
                {
                    bool reset = false;
                    foreach (var inf in roomData.playerInfo)
                    {
                        if (inf == 255)
                        {
                            reset = true;

                            break;
                        }
                    }

                    if (reset)
                    {
                        roomData.roomInfo = 0;

                        LobbyResetInfo();
                    }
                }

                NetInfo();
            }
        }

        public void NetInfo()
        {
            using (Message message = Message.Create((ushort)Shared.NetworkTags.ChangeLobbyInfo, roomData))
            {
                foreach (var client in clientConnections)
                {
                    client.client.SendMessage(message, SendMode.Reliable);
                }
            }
        }

        public void Join(ClientConnection a_connection)
        {
            clientConnections.Add(a_connection);

            a_connection.Room = this;

            // Finding a free spot (very useful)
            bool[] bad = new bool[4];

            foreach (var client in clientConnections)
            {
                if (client != a_connection)
                    bad[client.playerData.playerID] = true;
            }

            byte id = 0;
            for (int i = 0; i < bad.Length; i++)
            {
                if (!bad[i])
                {
                    id = (byte)i;

                    break;
                }
            }

            a_connection.playerData.playerID = id;
            //

            roomData.playerInfo[id] = 0;

            roomData.slots = (byte)clientConnections.Count;

            // Send player room information
            using (Message message = Message.Create((ushort)Shared.NetworkTags.JoinLobby, roomData))
            {
                a_connection.client.SendMessage(message, SendMode.Reliable);
            }

            LoadPlayer(a_connection);
        }

        public IEnumerator LoadPlayerCoroutine()
        {
            while (Base.PlayerLoader.Instance == null || roomData.mapAddress == "Lobby")
                yield return null;

            foreach (var client in clientConnections)
                LoadPlayer(client);

            yield break;
        }

        public void NetAll(Message a_message, SendMode a_reliability = SendMode.Reliable)
        {
            foreach (var connection in clientConnections)
            {
                connection.client.SendMessage(a_message, a_reliability);
            }
        }

        public void LoadPlayer(ClientConnection a_connection)
        {
            if (Base.PlayerLoader.Instance == null || roomData.mapAddress == "Lobby")
                return;

            Addressables.InstantiateAsync(Base.PlayerLoader.Instance.playerPrefab, Base.PlayerLoader.Instance.spawnPositions[a_connection.playerData.playerID].position, Quaternion.Euler(roomData.spawnRot), null, true).Completed += handle =>
            {
                GameObject result = handle.Result;

                MoveObjectTo(result);

                //Debug.LogError(result.name + " " + _scene.name);

                a_connection.controller = result;
                clientObjects[a_connection.client.ID] = result;

                Shared.NetworkedPlayerLogic character = result.AddComponent<Shared.NetworkedPlayerLogic>();
                character.ServerSetup(a_connection.playerData.playerID, this, a_connection);

                result.GetComponent<PlayerInput>()?.DeactivateInput();

                a_connection.playerData.state.position = result.transform.position;
                a_connection.playerData.state.rotation = result.transform.eulerAngles;

                using (Message message = Message.Create((ushort)Shared.NetworkTags.PlayerSpawn, a_connection.playerData))
                {
                    NetAll(message);
                }

                //Base.Players.players[a_connection.playerData.playerID] = new Base.PlayerData
                //{
                //    id = a_connection.client.ID,
                //    playerID = a_connection.playerData.playerID,
                //    player = result
                //};
            };
        }

        public void UpdateMasterPlayer(Shared.PlayerData a_data)
        {
            if (a_data.playerID == roomData.masterPlayer)
            {
                foreach (var client in clientConnections)
                {
                    if (client.playerData.clientID == a_data.clientID) continue;

                    roomData.masterPlayer = client.playerData.playerID;

                    Debug.LogError($"Lobby {roomData.roomID}'s master changed from Player {a_data.playerID} to Player {roomData.masterPlayer}.");

                    break;
                }
            }
        }

        public void Leave(ClientConnection a_connection)
        {
            using (Message message = Message.Create((ushort)Shared.NetworkTags.LeaveLobby, a_connection.playerData))
            {
                foreach (var player in clientConnections)
                {
                    player.client.SendMessage(message, SendMode.Reliable);
                }
            }

            roomData.playerInfo[a_connection.playerData.playerID] = 0;

            UpdateMasterPlayer(a_connection.playerData);

            if (roomData.slots < 2)
            {
                roomData.roomInfo = 0;

                LobbyResetInfo();
            }

            // Move player back to main menu

            if (clientObjects.ContainsKey(a_connection.client.ID))
                Destroy(clientObjects[a_connection.client.ID]);

            clientObjects.Remove(a_connection.client.ID);
            clientConnections.Remove(a_connection);

            a_connection.Room = null;

            roomData.slots = (byte)clientConnections.Count;

            NetInfo();

            Debug.LogError($"Player {a_connection.playerData.clientID} has left lobby {a_connection.playerData.lobby}." );
        }

        public void Close()
        {
            foreach (ClientConnection player in clientConnections)
            {
                Leave(player);
            }
            Destroy(gameObject);
        }

        public void Initialize(int a_id, string a_name, byte a_maxSlots, Vector3 a_position, Vector3 a_rotation)
        {
            roomData = new Shared.RoomData()
            {
                roomID = (ushort)a_id,
                name = a_name,
                maxSlots = a_maxSlots,
                masterPlayer = 0,
                roomInfo = 0,
                spawnPos = a_position,
                spawnRot = a_rotation,
                playerInfo = new byte[4]
            };

            Scene oldScene = SceneManager.GetSceneByName("Room_" + a_name);
            if (oldScene.IsValid())
            {
                SceneManager.UnloadSceneAsync(oldScene).completed += handle => {
                    CreateScene("Room_" + a_name);
                };
            }
            else
            {
                CreateScene("Room_" + a_name);
            }
        }

        public void PickMap()
        {
            Dictionary<int, int> maps = new Dictionary<int, int>();

            foreach (int i in roomData.playerInfo)
            {
                if (!maps.ContainsKey(i))
                    maps[i] = 0;

                maps[i]++;
            }

            int index = UnityEngine.Random.Range(0, maps.Count - 1);

            Debug.LogError($"Map index found {index} / {maps.Count - 1}");

            string map = Shared.NetworkInformation.Maps[index];

            Debug.LogError($"Map changing too {map}");

            RoomManager.Instance.UpdateRoomScene(this, map);
        }


        public void ReloadPlayers()
        {
            StartCoroutine(LoadPlayerCoroutine());
        }

        private void Update()
        {
            if (roomData.roomInfo == 1 && _waitTime != 0 && _waitTime < Time.realtimeSinceStartup)
            {
                Debug.LogError(_waitTime - Time.realtimeSinceStartup);
                _waitTime = 0;
                PickMap();
            }
        }

        private void FixedUpdate()
        {
            
        }

        private void CreateScene(string a_name)
        {
            CreateSceneParameters csp = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
            _scene = SceneManager.CreateScene(a_name, csp);
            physicsScene = _scene.GetPhysicsScene();

            MoveObjectTo(gameObject);
        }
    }
}
