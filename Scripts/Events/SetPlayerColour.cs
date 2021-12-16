using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AberrationGames
{
    public class SetPlayerColour : MonoBehaviour, Interfaces.IMenuInteraction
    {
        public int colourID = 0;
        public Image image;
        public Button button;
        public Image eyes;

        public Color GetColor()
        {
            return Settings.Instance.settingsReference.playerDataReference.playerColours.Length > colourID ? Settings.Instance.settingsReference.playerDataReference.playerColours[colourID] : image.color;
        }

        public bool ColorInUse()
        {
            foreach (var col in Players.PlayerDictionary)
            {
                if (col.Value.color == image.color)
                    return true;
            }

            return false;
        }

        public void ReleaseUIClick(MainMenuPlayer a_player)
        {
            if (!button.interactable)
                return;

            PlayerData data = Players.PlayerDictionary[a_player.playerIndex];
            data.color = GetColor();

            bool s = Players.PlayerDictionary[a_player.playerIndex].color == Color.white;

            Players.PlayerDictionary[a_player.playerIndex] = data;

            if (s)
                a_player.ClickPress(a_player.input, a_player.playerIndex);
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

        // Update is called once per frame
        void Update()
        {
            if (image != null)
                image.color = GetColor();

            button.interactable = !ColorInUse();
            eyes.gameObject.SetActive(button.interactable);
        }
    }
}
