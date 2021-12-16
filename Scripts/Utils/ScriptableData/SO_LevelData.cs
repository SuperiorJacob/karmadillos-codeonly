using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AberrationGames.Utils
{
    [System.Serializable]
    public struct SOLevelData
    {
        public string levelName;
        public Sprite levelIcon;
        public Scene levelScene;
        public Color levelColor;
    }


    [CreateAssetMenu(fileName = "LevelData", menuName = "Karma-Dillo/LevelData"),
        EditorTools.AberrationDescription("Simple level data class.", "Jacob Cooper", "14/11/2021")]
    public class SO_LevelData : ScriptableObject
    {
        public SOInputIcons[] inputIcons;
    }
}
