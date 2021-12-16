using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Utils
{
    [System.Serializable]
    public struct SOPlayerData
    {
        public string playerName;
        public Sprite playerIcon;
        [InspectorName("Default Color")] public Color color;
    }

    [System.Serializable]
    public struct SOInputIcons
    {
        public string deviceName;
        public Sprite deviceIcon;
    }

    [CreateAssetMenu(fileName = "PlayerData", menuName = "Karma-Dillo/PlayerData"),
            EditorTools.AberrationDescription("Player data storage class.", "Jacob Cooper", "14/11/2021")]
    public class SO_PlayerData : ScriptableObject
    {
        public SOInputIcons[] inputIcons;
        public SOPlayerData[] players;
        public Color[] playerColours;
    }
}
