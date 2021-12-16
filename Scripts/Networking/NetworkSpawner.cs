using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AberrationGames.Networking
{
    public class NetworkSpawner : MonoBehaviour
    {
        public static NetworkSpawner main;

        [SerializeField]
        [Tooltip("The client to communicate with the server via.")]
        private UnityClient client;

        [SerializeField]
        [Tooltip("The player object to spawn.")]
        private GameObject playerPrefab;

        [SerializeField]
        [Tooltip("The network player object to spawn.")]
        private GameObject networkPlayerPrefab;

        [SerializeField]
        [Tooltip("The network player manager.")]
        private NetworkManager networkManager;

        public string scene = "";

        private void Start()
        {
            main = this;
        }

        void Awake()
        {
            if (client == null)
            {
                Debug.LogError("No client assigned to NetworkSpawner component!");
                return;
            }

            client.MessageReceived += Client_MessageReceived;
            client.Disconnected += Client_Disconnected;
        }

        /// <summary>
        ///     Invoked when a message is received from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                //Spawn or despawn the player as necessary.
                if (message.Tag == (ushort)NetworkTags.SpawnPlayer)
                {
                    using (DarkRiftReader reader = message.GetReader())
                        SpawnPlayer(reader);
                }
                else if (message.Tag == (ushort)NetworkTags.DespawnSplayer)
                {
                    using (DarkRiftReader reader = message.GetReader())
                        DespawnPlayer(reader);
                }
            }
        }

        /// <summary>
        ///     Called when we disconnect from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            //If we disconnect then we need to destroy everything!
            networkManager.RemoveAllCharacters();
        }

        IEnumerator MountOntoPlayer(ushort a_id, ushort a_layer, int a_playerID, bool a_local, Vector3 a_position, Vector3 a_rotation)
        {
            while(Base.Players.ChangingScenes)
            {
                yield return new WaitForSeconds(.1f);
            }

            yield return new WaitForSeconds(.1f);

            if (a_local)
            {
                if (Base.Players.PlayerDictionary.ContainsKey(0))
                {
                    var ply = Base.Players.PlayerDictionary[0];

                    NetworkPlayer character = ply.player.AddComponent<NetworkPlayer>();
                    character.sendTransform = true;
                    character.localPlayer = true;
                    character.Setup(a_id, (NetworkLayer)a_layer);
                }
            }
            else
            {
                GameObject o = Instantiate(
                    networkPlayerPrefab,
                    a_position,
                    Quaternion.Euler(a_rotation)
                );

                NetworkPlayer character = o.GetComponent<NetworkPlayer>();
                character.Setup(a_id, (NetworkLayer)a_layer);
                networkManager.AddCharacter(a_id, character);
            }

        }

        private void CreateLocalPlayer(ushort a_id, ushort a_layer)
        {
            NetworkPlayer.client = client;

            SceneManager.LoadScene(1);

            Base.Players.ChangingScenes = true;

            StartCoroutine(MountOntoPlayer(a_id, a_layer, 0, true, Vector3.zero, Vector3.zero));
        }

        private void BuildMap(ushort a_id, ushort a_layer)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(a_id);
                writer.Write((ushort)NetworkTypes.Scene);
                writer.Write(scene);

                using (Message message = Message.Create((ushort)NetworkTags.Event, writer))
                    client.SendMessage(message, SendMode.Reliable);
            }

            CreateLocalPlayer(a_id, a_layer);
        }


        /// <summary>
        ///     Spawns a new player from the data received from the server.
        /// </summary>
        /// <param name="reader">The reader from the server.</param>
        void SpawnPlayer(DarkRiftReader reader)
        {
            //Extract the positions
            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Vector3 rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            //if (position == Vector3.zero || rotation == Vector3.zero)
            //{
            //    position = Base.PlayerLoader.Instance.spawnPosition.position;
            //    rotation = Base.PlayerLoader.Instance.spawnPositions.eulerAngles;
            //}

            ushort layer = reader.ReadUInt16();

            //Extract their ID
            ushort id = reader.ReadUInt16();

            string sceneName = reader.ReadString();

            //If it's a player for us then spawn us our prefab and set it up
            if (id == client.ID)
            {
                if ((NetworkLayer)layer == NetworkLayer.Master)
                {
                    BuildMap(id, layer);
                }
                else
                {
                    scene = sceneName;

                    CreateLocalPlayer(id, layer);
                }
            }
            //If it's for another player then spawn a network player and and to the manager. 
            else
            {
                StartCoroutine(MountOntoPlayer(id, layer, id, false, position, rotation));
            }
        }

        /// <summary>
        ///     Despawns and destroys a player from the data received from the server.
        /// </summary>
        /// <param name="reader">The reader from the server.</param>
        void DespawnPlayer(DarkRiftReader reader)
        {
            ushort id = reader.ReadUInt16();

            NetworkPlayer ply = networkManager.GetCharacter(id);

            if (ply != null && ply.gameObject != null)
            {
                ply.UnSetup();
                Destroy(ply.gameObject);
            }

            networkManager.RemoveCharacter(id);
        }
    }


}
