using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Networking.Shared
{
    [EditorTools.AberrationDescription("Pre-defined short network tags for communication.", "Jacob Cooper", "15/09/2021")]
    public enum NetworkTags : ushort
    {
        PlayerConnect = 0,
        PlayerDisconnect,
        JoinLobby,
        LobbyJoinSuccessful,
        LeaveLobby,
        ChangeLobbyInfo,
        LobbySelect,
        PlayerSpawn,
        Inputs,
        Data,
        Event
    }

    [EditorTools.AberrationDescription("Defined connection realm.", "Jacob Cooper", "15/09/2021")]
    public enum NetworkRealm : ushort
    {
        Local = 0,
        Server = 1
    }
}
