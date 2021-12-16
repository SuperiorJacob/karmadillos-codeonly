using DarkRift;
using DarkRift.Client.Unity;
using System.Collections.Generic;
using UnityEngine;


namespace AberrationGames.Networking
{
    public class NetworkPlayer : NetworkEntity
    {
        public static NetworkPlayer master = null;
        public static NetworkPlayer localClient = null;
        public static UnityClient client = null;

        public bool localPlayer = false;

        public Rigidbody rb;

        public int innerPlayerID;

        public ParticleSystem particle;
        public bool tempCharging;

        public void UnSetup()
        {
            Base.Players.PlayerDictionary.Remove(innerPlayerID);
        }

        public void Setup(ushort a_playerID, NetworkLayer a_layer)
        {
            this.uniqueID = a_playerID;
            this.networkLayer = a_layer;

            if (IsNetworkLayer(NetworkLayer.Master))
            {
                master = this;
            }

            if (localPlayer)
            {
                localClient = this;
            }

            TryGetComponent(out rb);
            TryGetComponent(out particle);
            // Alot of revamping required lmfao

            if (!localPlayer)
            {
                innerPlayerID = Base.Players.PlayerDictionary.Count;

                // Colouring
                var reference = Settings.Instance.settingsReference.playerDataReference;
                var playerRef = reference.players[Mathf.Clamp(innerPlayerID, 0, reference.players.Length - 1)];

                gameObject.GetComponent<Renderer>().material.color = new Color(playerRef.color.r, playerRef.color.g, playerRef.color.b, 0.3f);

                Base.Players.PlayerDictionary[innerPlayerID] = new Base.PlayerData
                {
                    id = innerPlayerID,
                    player = gameObject,
                    input = null,
                    device = null,
                    playerID = innerPlayerID + 1
                };
            }
            else
            {
                Base.Players.PlayerDictionary[0] = new Base.PlayerData
                {
                    id = 0,
                    player = gameObject,
                    input = Base.Players.PlayerDictionary[0].input,
                    device = Base.Players.PlayerDictionary[0].device,
                    playerID = 1
                };
            }
        }

        public override ushort GetUniqueID()
        {
            return 999;
        }

        public override void Start()
        {

        }

        public bool IsLocal()
        {
            return localPlayer;
        }

        public override void Update()
        {
            if (IsLocal() && ShouldSendTransform())
            {
                SendTransform(client);
            }
            if (!IsLocal())
            {
                UpdateTransform();
            }

            //base.Update();
        }

        public void NetworkForce(GameObject a_ent, Vector3 a_force)
        {
            if (a_ent.TryGetComponent(out NetworkPlayer player))
            {
                using (DarkRiftWriter writer = DarkRiftWriter.Create())
                {
                    writer.Write(player.uniqueID);
                    writer.Write((ushort)NetworkTypes.Rigidbody);
                    writer.Write(a_force.x);
                    writer.Write(a_force.y);
                    writer.Write(a_force.z);

                    using (Message message = Message.Create((ushort)NetworkTags.Event, writer))
                        client.SendMessage(message, SendMode.Reliable);
                }
            }
        }

        public void NetworkParticles(bool a_bool)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(uniqueID);
                writer.Write((ushort)NetworkTypes.Particles);
                writer.Write(a_bool);

                using (Message message = Message.Create((ushort)NetworkTags.Event, writer))
                    client.SendMessage(message, SendMode.Reliable);
            }
        }
    }
}
