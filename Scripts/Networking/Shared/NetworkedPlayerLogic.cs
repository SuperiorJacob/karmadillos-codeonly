using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DarkRift;

namespace AberrationGames.Networking.Shared
{
    //[RequireComponent(typeof(Base.PlayerBase))]
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug)]
    public class NetworkedPlayerLogic : MonoBehaviour
    {
        public Base.PlayerBase player { get; private set; }

        private PlayerInputData _playerInput;

        private ushort _playerID;

        // Server
        private bool _serverSided = false;
        private Server.Room _room;
        private Server.ClientConnection _connection;

        public void NetworkData()
        {

        }

        public void NetworkInputs()
        {
            //Debug.LogError("Sending inputs for p" + _playerID);

            using (Message sendMessage = Message.Create((ushort)NetworkTags.Inputs, _playerInput))
            {
                Client.ConnectionManager.Instance.Client.SendMessage(sendMessage, SendMode.Unreliable);
            }
        }

        public void SetInputs(Vector2 a_dir)
        {
            //Debug.Log("Moving: " + a_dir);

            _playerInput = new PlayerInputData(Client.ConnectionManager.Instance.Client.ID, 0, new PlayerInputs()
            {
                north = a_dir.y > 0,
                south = a_dir.y < 0,
                left = a_dir.x < 0,
                right = a_dir.x > 0
            });
        }

        public void NetworkUpdate()
        {
            Vector3 goal = Client.ConnectionManager.Instance.dataConnections[_playerID].state.position;
            Vector3 rotGoal = Client.ConnectionManager.Instance.dataConnections[_playerID].state.rotation;

            //if (Vector3.SqrMagnitude(transform.position - goal) > 0.1f ||
            //    Vector3.SqrMagnitude(transform.eulerAngles - rotGoal) > 5f)
            transform.position = Vector3.Lerp(transform.position, goal, Time.deltaTime * 10);
            transform.eulerAngles = rotGoal; // (Rotation lerping is weird in realtime physics) = Vector3.Lerp(transform.eulerAngles, rotGoal, Time.deltaTime * 10);
        }

        public void ServerSimulate()
        {
            PlayerInputs inputs = _connection.playerData.input.inputs;

            //player.GetRigidBody().AddForce(dir * 10);

            // Move the player based on their inputs.
            player.Movement(inputs.north ? 1f : (inputs.south ? -1f : 0f), // North
                inputs.left ? -1f : (inputs.right ? 1f : 0f)); // Right

            // Update the snapshot.
            _connection.playerData.state.position = transform.position;
            _connection.playerData.state.rotation = transform.eulerAngles;

            using (Message message = Message.Create((ushort)Shared.NetworkTags.Data, _connection.playerData))
            {
                //Debug.Log("Spawning Player: " + _connection.playerData.playerID);
                _room.NetAll(message, SendMode.Unreliable);
            }
        }

        public void Setup(byte a_playerID)
        {
            _playerID = a_playerID;

            if (NetworkInformation.Realm == NetworkRealm.Local)
            {
                // Colouring
                var reference = Settings.Instance.settingsReference.playerDataReference;
                var playerRef = reference.players[Mathf.Clamp(a_playerID, 0, reference.players.Length - 1)];

                gameObject.GetComponentInChildren<Renderer>().material.color = new Color(playerRef.color.r, playerRef.color.g, playerRef.color.b, 0.3f);

                Base.Players.PlayerDictionary[a_playerID] = new Base.PlayerData
                {
                    id = Client.ConnectionManager.Instance.playerData.clientID,
                    player = gameObject,
                    input = null,
                    device = null,
                    playerID = a_playerID
                };
            }
            else
                _serverSided = true;

            //if (Base.PlayerLoader.main != null)
                //Base.PlayerLoader.main.LoadPlayers();
        }

        public void ServerSetup(byte a_playerID, Server.Room a_room, Server.ClientConnection a_clientConnection)
        {
            _room = a_room;
            _connection = a_clientConnection;

            Setup(a_playerID);
        }

        void Start()
        {
            player = GetComponent<Base.PlayerBase>();
        }

        private void Update()
        {
            if (!_serverSided)
            {
                NetworkUpdate();
            }
                
        }

        void FixedUpdate()
        {
            if (_serverSided && NetworkInformation.Realm == NetworkRealm.Server)
            {
                ServerSimulate();
            }
            else if (!_serverSided && NetworkInformation.Realm == NetworkRealm.Local && _playerID == Client.ConnectionManager.Instance.playerData.playerID)
            {
                //NetworkUpdate();

                //Debug.Log(_playerID);

                SetInputs(player.StickDir);
                NetworkInputs();
            }

            //NetworkFixedUpdate();
        }
    }
}
