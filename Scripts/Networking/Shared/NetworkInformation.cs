using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Networking.Shared
{
    public static class NetworkInformation
    {
        // What realm are we in? Server or Client
        public static NetworkRealm Realm;
        // Are we currently experiencing networking (is this multiplayer).
        public static bool IsNetworking = false;
        // Should we spawn the local player? (used for lobbies).
        public static bool ShouldSpawnLocal = false;

        // Remove later
        public static float WaitTime = 5f;

        // Predefined available maps, very useful, switch to scriptable object later.
        public static string[] Maps = new string[] 
        {
            "Level_LilyPad",
            "Level_Rooftops",
            "Level_TidesUp",
            "Level_Bathroom",
            "Level_Elevator",
            "Level_Sumo",
        };
    }
}
