using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;

namespace AberrationGames.Networking.Shared
{
    // An event manager that is used everywhere smile
    public class EventManager : MonoBehaviour
    {
        public Dictionary<byte, System.Action<object>> roomEvents;

        void Awake()
        {
            roomEvents = new Dictionary<byte, System.Action<object>>();
        }

        public void CallEvent(byte a_event, object a_param, bool a_networked = false)
        {
            roomEvents[a_event].Invoke(a_param);

            if (a_networked)
            {
                using (Message message = Message.CreateEmpty((ushort)NetworkTags.Event))
                {
                    using (DarkRiftWriter writer = DarkRiftWriter.Create())
                    {
                        writer.Write(a_event);
                        message.Serialize(writer);
                    }

                    if (NetworkInformation.Realm == NetworkRealm.Server)
                    {
                        foreach (var client in Server.ServerManager.Instance.players)
                            client.Value.client.SendMessage(message, SendMode.Reliable);
                    }
                    else
                    {
                        Client.ConnectionManager.Instance.Client.SendMessage(message, SendMode.Reliable);
                    }
                }
            }
        }
    }
}
