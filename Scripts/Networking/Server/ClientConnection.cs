using DarkRift;
using DarkRift.Server;

namespace AberrationGames.Networking.Server
{
    public class ClientConnection
    {
        public IClient client;
        public Shared.PlayerData playerData;

        public UnityEngine.GameObject controller;
        public Base.PlayerBase player;

        public Room Room { get; set; }

        private void OnMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage())
            {
                switch ((Shared.NetworkTags)message.Tag)
                {
                    case Shared.NetworkTags.Inputs:
                        Shared.PlayerInputData input = message.Deserialize<Shared.PlayerInputData>();
                        playerData.input = input;

                        //UnityEngine.Debug.Log("Updating inputs for " + playerData.playerID);

                        if (player != null)
                        {
                            //player.Movement(
                            //    input.inputs.north ? 1 : (input.inputs.south ? -1 : 0),
                            //    input.inputs.left ? 1 : (input.inputs.right ? -1 : 0)
                            //);
                        }
                        else
                            player = controller.GetComponent<Base.PlayerBase>();

                        break;
                    case Shared.NetworkTags.LobbyJoinSuccessful:
                        Room.RelayClient(this);

                        break;
                    case Shared.NetworkTags.LobbySelect:
                        Room.LobbySelect(this, message);

                        break;
                    case Shared.NetworkTags.Event:


                        break;
                    default:
                        break;
                }
            }
        }

        public void OnClientDisconnect(object sender, ClientDisconnectedEventArgs e)
        {
            if (Room != null)
                Room.Leave(this);

            ServerManager.Instance.players.Remove(client.ID);

            e.Client.MessageReceived -= OnMessage;
        }

        public void ConnectToRoom(Room a_room)
        {
            RoomManager.Instance.TryJoinRoom(client, a_room);
        }

        public ClientConnection(IClient a_client, Shared.PlayerData a_playerData)
        {
            client = a_client;
            playerData = a_playerData;

            ServerManager.Instance.players[a_client.ID] = this;

            Room room;

            if (a_playerData.lobby >= RoomManager.Instance.rooms.Count)
            {
                RoomManager.Instance.CreateRoom($"CLIENT {a_client.ID}'s Lobby", RoomManager.Instance.tempRoomReference, 4, ConnectToRoom);
                return;
            }
            else
            {
                room = RoomManager.Instance.rooms[a_playerData.lobby];
                ConnectToRoom(room);
            }

            client.MessageReceived += OnMessage;
        }
    }
}
