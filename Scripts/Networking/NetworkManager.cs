using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AberrationGames.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        /// <summary>
        ///     The unit client we communicate via.
        /// </summary>
        [SerializeField]
        [Tooltip("The client to communicate with the server via.")]
        UnityClient client;

        /// <summary>
        ///     The characters we are managing.
        /// </summary>
        Dictionary<ushort, NetworkPlayer> characters = new Dictionary<ushort, NetworkPlayer>();

        void Awake()
        {
            if (client == null)
            {
                Debug.LogError("No client assigned to NetworkSpawner component!");
                return;
            }

            client.MessageReceived += Client_MessageReceived;
        }

        /// <summary>
        ///     Called when a message is received from the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                //Check the tag
                if (message.Tag == (ushort)NetworkTags.Movement)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        //Read message
                        Vector3 newPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        Vector3 newRotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        ushort layer = reader.ReadUInt16();
                        ushort id = reader.ReadUInt16();

                        if (characters.ContainsKey(id))
                        {
                            //Update characters to move to new positions
                            characters[id].NewPosition = newPosition;
                            characters[id].NewRotation = newRotation;
                        }
                    }
                }
                else if (message.Tag == (ushort)NetworkTags.Event)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        NetworkTypes networkType = (NetworkTypes)reader.ReadUInt16();

                        if (networkType == NetworkTypes.Rigidbody)
                        {
                            Vector3 force = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                            NetworkPlayer.localClient.rb.AddForce(force, ForceMode.Impulse);
                        }
                        else if (networkType == NetworkTypes.Particles)
                        {
                            bool particle = reader.ReadBoolean();
                            NetworkPlayer.localClient.tempCharging = particle;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Adds a character to the list of those we're managing.
        /// </summary>
        /// <param name="id">The ID of the owning player.</param>
        /// <param name="character">The character to synchronize.</param>
        public void AddCharacter(ushort id, NetworkPlayer character)
        {
            characters.Add(id, character);
        }

        /// <summary>
        ///     Removes a character from the list of those we're managing.
        /// </summary>
        /// <param name="id">The ID of the owning player.</param>
        public void RemoveCharacter(ushort id)
        {
            Destroy(characters[id].gameObject);
            characters.Remove(id);
        }

        public NetworkPlayer GetCharacter(ushort id)
        {
            return characters.ContainsKey(id) ? characters[id] : null;
        }

        public Dictionary<ushort, NetworkPlayer> GetAll()
        {
            return characters;
        }

        /// <summary>
        ///     Removes all characters that are being managded.
        /// </summary>
        internal void RemoveAllCharacters()
        {
            foreach (NetworkPlayer character in characters.Values)
                Destroy(character.gameObject);

            characters.Clear();
        }
    }


}
