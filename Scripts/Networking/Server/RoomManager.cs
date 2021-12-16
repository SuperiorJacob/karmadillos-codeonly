using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AberrationGames.Networking.Server
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug, "d_AudioEchoFilter Icon")]
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Instance;

        // Temporary :)
        [EditorTools.AberrationRequired] public string tempRoomReference;

        public List<Room> rooms = new List<Room>();
        public Dictionary<string, Room> roomsByName = new Dictionary<string, Room>();

        //

        public Room InitRoom(GameObject a_object, string a_roomName, string a_roomReference, byte a_maxSlots)
        {
            Vector3 spawnPos = Vector3.zero;
            Vector3 spawnRot = Vector3.zero;

            Base.PlayerLoader loader = a_object.transform.GetComponentInChildren<Base.PlayerLoader>();
            if (loader != null)
            {
                spawnPos = loader.spawnPositions[0].position;
                spawnRot = loader.spawnPositions[0].eulerAngles;
            }

            Room room = a_object.AddComponent<Room>();
            room.Initialize(rooms.Count, a_roomName, a_maxSlots, spawnPos, spawnRot);
            room.roomData.mapAddress = a_roomReference;

            rooms.Add(room);
            roomsByName.Add(a_roomName, room);

            return room;
        }

        public void RecreateRoom(Room a_room, GameObject a_object, string a_name, string a_roomReference, byte a_maxSlots)
        {
            a_room.MoveObjectTo(a_object);

            Vector3 spawnPos = Vector3.zero;
            Vector3 spawnRot = Vector3.zero;

            Base.PlayerLoader loader = a_object.transform.GetComponentInChildren<Base.PlayerLoader>();
            if (loader != null)
            {
                spawnPos = loader.spawnPositions[0].position;
                spawnRot = loader.spawnPositions[0].eulerAngles;
            }

            a_room.roomData.name = a_name;
            a_room.roomData.spawnPos = spawnPos;
            a_room.roomData.spawnRot = spawnRot;
            a_room.roomData.roomInfo = 0;
            a_room.roomData.mapAddress = a_roomReference;


            Debug.Log("Reloading room");
            a_room.ReloadPlayers();
            a_room.NetInfo();
        }

        public void UpdateRoomScene(Room a_room, string a_roomReference)
        {
            if (string.IsNullOrEmpty(a_roomReference))
                RecreateRoom(a_room, new GameObject(a_room.roomData.name + "_Lobby"), a_room.roomData.name, "Lobby", a_room.roomData.maxSlots);
            else
                Addressables.InstantiateAsync(a_roomReference).Completed += handle =>
                    RecreateRoom(a_room, handle.Result, a_room.roomData.name, a_roomReference, a_room.roomData.maxSlots);
        }

        public void CreateRoom(string a_roomName, string a_roomReference, byte a_maxSlots, System.Action<Room> a_action = null)
        {
            if (string.IsNullOrEmpty(a_roomReference))
            {
                Room room = InitRoom(new GameObject(a_roomName), a_roomName, "Lobby", a_maxSlots);

                if (a_action != null)
                    a_action.Invoke(room);
            }
            else
                Addressables.InstantiateAsync(a_roomReference).Completed += handle =>
                {
                    Room room = InitRoom(handle.Result, a_roomName, a_roomReference, a_maxSlots);

                    if (a_action != null)
                        a_action.Invoke(room);
                };

            APIUtility.Create();
            APIUtility.Instance.AddLobby(a_roomName, "0", "0", a_maxSlots.ToString(), APIUtility.GetIPAddress(), a_roomReference);
        }

        public void RemoveRoom(string a_room)
        {
            Room room = roomsByName[a_room];
            room.Close();

            rooms.Remove(roomsByName[a_room]);
            roomsByName.Remove(a_room);
        }

        public void RemoveRoom(int a_room)
        {
            Room room = rooms[a_room];
            room.Close();

            roomsByName.Remove(room.name);
            rooms.Remove(room);
        }

        public void TryJoinRoom(IClient a_client, string a_roomName)
        {
            bool canJoin = ServerManager.Instance.players.TryGetValue(a_client.ID, out ClientConnection clientConnection);

            if (!roomsByName.TryGetValue(a_roomName, out Room room))
                canJoin = false;
            else if (room.clientConnections.Count >= room.roomData.maxSlots)
                canJoin = false;

            if (canJoin)
                room.Join(clientConnection);
            else
            {
                using (Message m = Message.CreateEmpty((ushort)Shared.NetworkTags.LeaveLobby))
                {
                    a_client.SendMessage(m, SendMode.Reliable);
                }
            }
        }

        public void TryJoinRoom(IClient a_client, Room a_room)
        {
            bool canJoin = ServerManager.Instance.players.TryGetValue(a_client.ID, out ClientConnection clientConnection);

            if (a_room.clientConnections.Count >= a_room.roomData.maxSlots)
                canJoin = false;

            if (canJoin)
                a_room.Join(clientConnection);
            else
            {
                using (Message m = Message.CreateEmpty((ushort)Shared.NetworkTags.LeaveLobby))
                {
                    a_client.SendMessage(m, SendMode.Reliable);
                }
            }
        }

        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(this);

            // temp
            CreateRoom("Main", "", 4);
            //CreateRoom("Main 2", tempRoomReference, 4);
        }

        private void FixedUpdate()
        {
            foreach (var room in rooms)
            {
                room.physicsScene.Simulate(Time.fixedDeltaTime);
            }
        }
    }
}
