using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Discord;
using System;

namespace AberrationGames.Utils
{
    [System.Serializable]
    public struct RPCMap
    {
        [Tooltip("Addressable name.")] public string name;
        [Tooltip("Icon name on discord.")] public string discordName;
    }

    [System.Serializable]
    public struct RPCRule
    {
        [Tooltip("Top Row")] public string details;
        [Tooltip("Second Row")] public string state;
        public string largeImage;
        public string largeImageText;
        public string smallImage;
        public string smallImageText;
    }

    [System.Serializable]
    public struct RPCSettings
    {
        [Header("Rules")]
        public RPCRule mainMenu;
        public RPCRule localPlay;
        public string inGameState;

        [Header("Maps")]
        public RPCMap[] maps;
    }

    public static class RichPresenceHandler
    {
        public static Discord.Discord Handle;

        public static RPCSettings RpcSettings;

        public static long OldEpochTime;

        public static void Init()
        {
            RpcSettings = Settings.Instance.settingsReference.rpcSettings;

            long id = Settings.Instance.settingsReference.discordRPCID;

            try
            {
                Handle = new Discord.Discord(id, (ulong)CreateFlags.NoRequireDiscord);
            }
            catch
            {
                Debug.LogWarning("User does not have discord");
            }

            RPCRule menu = RpcSettings.mainMenu;

            // We love defaulting :')
            UpdateActivity(menu, 4, 0, "", 
                default, default, default, true);
        }

        public static void Stop()
        {
            if (Handle == null)
                return;

            Handle.Dispose();
        }

        public static void UpdateActivity(string a_state, string a_details, string a_largeImage, 
            string a_largeImageText, string a_smallImage, string a_smallImageText, int a_partyMax = 4,
            int a_partySize = 0, string a_partyID = "", string a_join = @"1114151244fa1", string a_spectate = @"1114151244fa2", string a_match = @"1114151244fa", bool a_setTime = false)
        {
            if (Handle == null)
                return;

            // Setting start time
            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            long epochTime = (long)(System.DateTime.UtcNow - epochStart).TotalSeconds;

            if (a_setTime)
                OldEpochTime = epochTime;

            var activityManager = Handle.GetActivityManager();
            var activity = new Discord.Activity
            {
                State = a_state,
                Details = a_details,
                Assets = {
                    LargeImage = a_largeImage,
                    LargeText = a_largeImageText,
                    SmallImage = a_smallImage,
                    SmallText = a_smallImageText
                },
                Timestamps =
                {
                    Start = a_setTime ? epochTime : OldEpochTime,
                    End = 0
                },
                Instance = false
            };

            if (!string.IsNullOrEmpty(a_partyID))
            {
                activity.Party = new ActivityParty {
                    Id = a_partyID,
                    Size = {
                        CurrentSize = a_partySize,
                        MaxSize = a_partyMax
                    }
                };

                activity.Secrets = new ActivitySecrets
                {
                    Join = a_join,
                    Spectate = a_spectate,
                    Match = a_match
                };
            }

            activityManager.UpdateActivity(activity, (res) =>
            {
                if (res == Discord.Result.Ok)
                {
                    Debug.Log("Successful Discord Presence");
                }
                else
                {
                    Debug.Log("RPC Failed, reason: " + res.ToString());
                }
            });
        }

        public static void UpdateActivity(RPCRule a_rule, int a_maxPartySize = 4, int a_partySize = 0, string a_partyID = "", string a_join = "1412414124", string a_spectate = "124124241", string a_match = "14141562", bool a_setTime = false)
        {
            UpdateActivity(a_rule.state, a_rule.details, a_rule.largeImage, a_rule.largeImageText, a_rule.smallImage, a_rule.smallImageText, a_maxPartySize,
                a_partySize, a_partyID, a_join, a_spectate, a_match, a_setTime);
        }

        internal static string GetMap(string a_scene)
        {
            string m = "lilypad";

            foreach (var map in RpcSettings.maps)
            {
                if (map.name == a_scene)
                    m = map.discordName;
            }
            return m;
        }
    }
}
