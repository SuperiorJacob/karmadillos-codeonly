using DarkRift;
using DarkRift.Client.Unity;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Networking
{
    public class NetworkEntity : MonoBehaviour
    {
        public static Dictionary<ushort, NetworkEntity> networkEntities = new Dictionary<ushort, NetworkEntity>();

        [Header("Network Information")]
        public ushort uniqueID = 0;
        public NetworkLayer networkLayer;
        public bool sendTransform = false;
        public bool sendAnimation = false;

        [Header("Update Settings")]
        [Tooltip("The speed to lerp the entities position")]
        public float moveLerpSpeed = 10f;

        [Tooltip("The speed to lerp the entities rotation")]
        public float rotateLerpSpeed = 10f;

        public Vector3 NewPosition { get; set; }
        
        public Vector3 NewRotation { get; set; }

        public virtual void Start()
        {
            networkEntities[uniqueID] = this;
        }

        public void Awake()
        {
            //Set initial values
            NewPosition = transform.position;
            NewRotation = transform.eulerAngles;
        }

        /// <summary>
        /// Updates the transform information relevent to the network information.
        /// </summary>
        public void UpdateTransform()
        {
            transform.position = Vector3.Lerp(transform.position, NewPosition, Time.deltaTime * moveLerpSpeed);
            transform.eulerAngles = new Vector3(
                Mathf.LerpAngle(transform.eulerAngles.x, NewRotation.x, Time.deltaTime * rotateLerpSpeed),
                Mathf.LerpAngle(transform.eulerAngles.y, NewRotation.y, Time.deltaTime * rotateLerpSpeed),
                Mathf.LerpAngle(transform.eulerAngles.z, NewRotation.z, Time.deltaTime * rotateLerpSpeed)
            );
        }

        public virtual ushort GetUniqueID()
        {
            return uniqueID;
        }

        public void SendTransform(UnityClient a_client)
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(GetUniqueID());
                writer.Write(transform.position.x);
                writer.Write(transform.position.y);
                writer.Write(transform.position.z);
                writer.Write(transform.eulerAngles.x);
                writer.Write(transform.eulerAngles.y);
                writer.Write(transform.eulerAngles.z);

                using (Message message = Message.Create((ushort)NetworkTags.Movement, writer))
                    a_client.SendMessage(message, SendMode.Unreliable);
            }

            NewPosition = transform.position;
            NewRotation = transform.eulerAngles;
        }

        public bool IsNetworkLayer(NetworkLayer a_layer)
        {
            return this.networkLayer == a_layer;
        }

        public bool SameEntity(NetworkEntity a_otherEnt)
        {
            return a_otherEnt != null && this.uniqueID == a_otherEnt.uniqueID;
        }

        public bool ShouldSendTransform()
        {
            return (sendTransform && 
                Vector3.SqrMagnitude(transform.position - NewPosition) > 0.1f ||
                Vector3.SqrMagnitude(transform.eulerAngles - NewRotation) > 5f);
        }

        public virtual void Update()
        {
            if (NetworkPlayer.master == null) return;

            if (SameEntity(NetworkPlayer.master) && ShouldSendTransform())
            {
                SendTransform(NetworkPlayer.client);
            }
            else
            {
                UpdateTransform();
            }
        }
    }
}
