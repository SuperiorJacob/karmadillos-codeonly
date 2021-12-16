using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("UI Script for loading player data onto the canvas.", "Jacob Cooper", "15/10/2021")]
    public class PlayerCard : MonoBehaviour
    {
        [Header("Player Info")]
        public int playerID;
        public PlayerData connectedPlayer;
        public bool connected = false;
        public bool isMaster = false;
        public bool ready = false;

        [Header("Load")]
        public Image background;
        public TMP_Text info;
        public Image icon;
        public Image topIcon;
        public string device = "";
        public NumberDisplay numberDisplay;
        public Button readyButton;

        [Header("Language Settings")]
        public string titleBase = "Player";
        public string notJoinedInfo = "Press any key to join.";
        public string joinedInfo = "Device:";

        public void Start()
        {
            if (Networking.Shared.NetworkInformation.IsNetworking && Networking.Shared.NetworkInformation.Realm == Networking.Shared.NetworkRealm.Local)
            {
                notJoinedInfo = "Waiting for another player...";

                Networking.Shared.PlayerData pData = Networking.Client.ConnectionManager.Instance.playerData;
                
                if (pData.playerID == playerID)
                {
                    connected = true;
                    device = pData.device;
                }
            }
        }

        public void ReadyToggle(PlayerInput a_input, int a_playerID)
        {
            if (a_playerID != playerID)
                return;

            ready = !ready;

            numberDisplay.number += (ready ? 1 : -1);

            readyButton.interactable = numberDisplay.number == numberDisplay.secondNumber;
        }

        public void OnEnable()
        {
            FindPlayer(playerID);

            readyButton.interactable = numberDisplay.number == numberDisplay.secondNumber;

            PlayerLoader.Instance.onClickPress.AddListener(ReadyToggle);
        }

        public void OnDisable()
        {
            PlayerLoader.Instance.onClickPress.RemoveListener(ReadyToggle);
        }

        public void FindPlayer(int a_index)
        {
            if (Players.PlayerDictionary != null && Players.PlayerDictionary.ContainsKey(a_index))
            {
                PlayerData pData = Players.PlayerDictionary[a_index];

                if (pData.playerID == playerID)
                {
                    connectedPlayer = pData;

                    if (!connected)
                        numberDisplay.secondNumber = Players.PlayerDictionary.Count;

                    connected = true;

                    device = connectedPlayer.input != null ? connectedPlayer.input.devices[0].displayName : device;
                    //a_input.uiInputModule = EventSystem.current.GetComponent<>;

                    Utils.RichPresenceHandler.UpdateActivity(
                        Utils.RichPresenceHandler.RpcSettings.localPlay, 4, Players.PlayerDictionary.Count, "148u1");
                }

            }
        }

        public void FindPlayer(PlayerInput a_input)
        {
            FindPlayer(a_input.user.index - 1);
        }

        private Color _innerColor;

        public void Update()
        {
            if (connected)
            {
                if (Players.PlayerDictionary.ContainsKey(playerID))
                    _innerColor = Players.PlayerDictionary[playerID].color;

                topIcon.color = _innerColor;
                background.color = _innerColor;

                bool shouldBeActive = false;
                bool setDevice = false;
                foreach (var deviceIcon in Settings.Instance.settingsReference.playerDataReference.inputIcons)
                {
                    if (deviceIcon.deviceName.ToLower() == device.ToLower())
                    {
                        setDevice = true;

                        if (Networking.Shared.NetworkInformation.IsNetworking &&
                            Networking.Shared.NetworkInformation.Realm == Networking.Shared.NetworkRealm.Local
                            && Networking.Client.ConnectionManager.Instance.playerData.playerID == playerID)
                        {
                            info.text = "YOU";
                            shouldBeActive = true;
                        }
                        else
                        {
                            info.text = "";
                            shouldBeActive = false;
                        }

                        if (isMaster)
                        {
                            shouldBeActive = true;

                            if (info.text.Length > 0)
                                info.text += " (Master)";
                            else
                                info.text = "(Master)";
                        }

                        icon.gameObject.SetActive(true);

                        icon.sprite = deviceIcon.deviceIcon;
                        icon.color = _innerColor;

                        break;
                    }
                }

                if (!setDevice)
                    info.text = joinedInfo + " " + device;

                if (ready)
                {
                    shouldBeActive = true;

                    if (info.text.Length > 0)
                        info.text += " (Ready)";
                    else
                        info.text = "(Ready)";
                }

                info.gameObject.SetActive(shouldBeActive);
            }
            else
            {
                info.gameObject.SetActive(true);
                icon.gameObject.SetActive(false);

                info.text = notJoinedInfo;

                background.color = Color.clear;
            }
        }
    }
}
