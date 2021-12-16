using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Displaying voted map icons and UI events.", "Jacob Cooper", "14/11/2021")]
    public class VotedMap : MonoBehaviour, Interfaces.IMenuInteraction
    {
        public string map;
        public Utils.ChangeScene sceneChanger;
        public Image[] icons;

        [HideInInspector] public MapVote backReference;
        [HideInInspector] public int index;

        public void Goto()
        {
            sceneChanger.SetScene(map);
        }

        public void ReleaseUIClick(MainMenuPlayer a_player)
        {
            backReference.SelectMap(a_player.playerIndex, index);
        }

        public void ReloadClick(MainMenuPlayer a_player, float a_reloadAmount = 1)
        {
        }

        public void SecondUIClick(MainMenuPlayer a_player)
        {
        }

        public void SetUIClick(MainMenuPlayer a_player)
        {
        }
    }
}
