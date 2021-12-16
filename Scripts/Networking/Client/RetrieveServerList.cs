using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using SimpleJSON;
using UnityEngine.UI;
using TMPro;

namespace AberrationGames.Networking.Client
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug)]
    public class RetrieveServerList : MonoBehaviour
    {
        [SerializeField] private Transform _serverList;
        [SerializeField] private AssetReference _serverDisplayPrefab;

        private readonly string _aberrationLobbyURI = "https://api.aberrationgames.net/api/v1/server/fetch-lobbies";
        private Dictionary<GameObject, JSONNode> _list;

        private bool _cannotRequest = false;

        public void DisplayLobbyList()
        {
            if (_cannotRequest)
                return;

            foreach (GameObject child in _list.Keys)
            {
                Destroy(child);
            }

            StartCoroutine(RequestLobbyList());
        }

        private IEnumerator GetPing(TMP_Text a_display, string a_address)
        {
            WaitForSeconds f = new WaitForSeconds(0.05f);

            // This will tell you how long the ping has taken!
            Ping p = new Ping(a_address);

            while (!p.isDone)
            {
                yield return f;
            }

            a_display.text += $" |  {p.time}ms";
        }

        private void ButtonClicked(int a_lobby)
        {

        }

        private IEnumerator RequestLobbyList()
        {
            UnityWebRequest serverListRequest = UnityWebRequest.Get(_aberrationLobbyURI);

            yield return serverListRequest.SendWebRequest();

            if (serverListRequest.result == UnityWebRequest.Result.ConnectionError ||
                serverListRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(serverListRequest.error);
                yield break;
            }

            JSONNode serverInfo = JSON.Parse(serverListRequest.downloadHandler.text);

            int count = 1;
            foreach (var lobby in serverInfo["lobbies"])
            {
                Addressables.InstantiateAsync(_serverDisplayPrefab, _serverList, true).Completed += handle =>
                {
                    JSONNode lobbyNode = lobby.Value;

                    GameObject obj = handle.Result;

                    if (obj.TryGetComponent(out Button button))
                    {
                        int lobby = lobbyNode["lobby"].AsInt;
                        button.onClick.AddListener(
                            () => {
                                ButtonClicked(lobby);
                            });
                    }

                    if (obj.TryGetComponent(out RectTransform rect))
                    {
                        rect.localScale = Vector2.one;

                        if (_serverList.TryGetComponent(out RectTransform serverListRect))
                        {
                            rect.localPosition = new Vector2(serverListRect.rect.width / 2, -25 + (count * -50));

                            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, serverListRect.rect.width);
                            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
                        }
                    }

                    _list[obj] = lobbyNode;

                    TMP_Text display = obj.GetComponentInChildren<TMP_Text>();
                    if (display != null)
                    {
                        string lobbyName = lobbyNode["lobby_name"].Value;
                        string mapName = lobbyNode["map_name"].Value;
                        string serverIP = lobbyNode["server_ip"].Value;

                        int maxPlayers = lobbyNode["max_players"].AsInt;
                        int playerCount = lobbyNode["player_count"].AsInt;

                        display.text = $"{lobbyName}  |  {mapName}  |  {playerCount} / {maxPlayers}";

                        StartCoroutine(GetPing(display, serverIP));
                    }

                    count++;
                };
            }

            _cannotRequest = true;

            yield return new WaitForSeconds(2f);

            _cannotRequest = false;
        }

        private void Awake()
        {
            _list = new Dictionary<GameObject, JSONNode>();
        }

        private void Start()
        {
            DisplayLobbyList();
        }
    }
}
