using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AberrationGames.Networking.Client
{
    [System.Serializable]
    public struct TempMapData
    {
        public string name;
        public TMP_Text text;
        public int voted;
    }

    public class UpdateSelection : MonoBehaviour
    {
        public static UpdateSelection Instance;

        public TMP_Text selectButton = null;
        public GameObject next = null;
        public GameObject back = null;
        public TempMapData[] maps;

        private int _selection = 0;
        private int _counted = 0;

        public void OnEnable()
        {
            Instance = this;
            _selection = 0;
            _counted = 0;
        }

        public void UpdateReadyUp()
        {
            Events.PlayerCard[] cards = new Events.PlayerCard[4];
            Base.PlayerLoader.Instance.canvas.GetComponentsInChildren<Events.PlayerCard>().CopyTo(cards, 0);

            int count = 0;
            for (int i = 0; i < Client.ConnectionManager.Instance.roomData.playerInfo.Length; i++)
            {
                byte info = Client.ConnectionManager.Instance.roomData.playerInfo[i];
                if (info == 1)
                {
                    cards[i].ready = true;
                    count++;
                }
                else cards[i].ready = false;
            }

            _counted = count;
        }

        public void ReadyUp()
        {
            if (Client.ConnectionManager.Instance != null && Client.ConnectionManager.Instance.roomData.roomInfo == 0)
            {
                _selection = _selection == 1 ? 0 : 1;
                Client.ConnectionManager.Instance.SetClientSelection((byte)_selection);

                UpdateReadyUp();
            }
            else
            {
                // Do this for local later
            }
        }

        public void VoteForMap(int a_map)
        {
            if (Client.ConnectionManager.Instance != null && Client.ConnectionManager.Instance.roomData.roomInfo == 1)
            {
                Client.ConnectionManager.Instance.SetClientSelection((byte)a_map);
            }
            else
            {
                // local shit
            }
        }

        private void VoteMap(byte a_map)
        {
            TempMapData map = maps[a_map];
            map.text.text = $"{map.name} | {map.voted}";
        }
        
        public void UpdateVotes()
        {
            // Could revamp smile
            for (int i = 0; i < maps.Length; i++)
            {
                maps[i].voted = 0;
            }

            foreach (byte info in Client.ConnectionManager.Instance.roomData.playerInfo)
            {
                if ((info - 1) > maps.Length || info == 0)
                    continue;

                maps[(info - 1)].voted += 1;
            }
        }

        private void FixedUpdate()
        {
            if (Client.ConnectionManager.Instance != null)
            {
                if (Client.ConnectionManager.Instance.roomData.roomInfo == 0)
                {
                    UpdateReadyUp();

                    byte tim = Client.ConnectionManager.Instance.roomData.slots;
                    if (tim == 1)
                        tim = 2;

                    selectButton.text = $"{_counted}/{tim}";
                }
                else if (Client.ConnectionManager.Instance.roomData.roomInfo == 1)
                {
                    for (int i = 0; i < maps.Length; i++)
                    {
                        VoteMap((byte)i);
                    }
                }
            }
        }
    }
}
