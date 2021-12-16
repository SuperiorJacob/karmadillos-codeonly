using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AberrationGames.Utils
{
    [System.Serializable]
    public struct SOADRESSPlayerData
    {
        public string playerName;
        public AssetReference playerSprite;
        public Color color;
    }

    [System.Serializable]
    public struct SOADRESSInputIcons
    {
        public string deviceName;
        public AssetReference deviceSprite;
    }

    [CreateAssetMenu(fileName = "PlayerData", menuName = "Karma-Dillo/Addressable_PlayerData"),
        EditorTools.AberrationDescription("Player addressable data scriptable object.", "Jacob Cooper", "14/11/2021")]
    public class SO_ADRESS_PlayerData : ScriptableObject
    {
        public AssetReference playerReference;
        public SOADRESSInputIcons[] inputIcons;
        public SOADRESSPlayerData[] players;
    }
}
