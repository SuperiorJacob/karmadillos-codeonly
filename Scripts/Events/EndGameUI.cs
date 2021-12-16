using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AberrationGames.Events
{
    [System.Serializable]
    public struct EndGameContainerData
    {
        public GameObject container;
        public GameObject winContainer;
        public TMP_Text score;
        public TMP_Text name;
        public SkinnedMeshRenderer[] shell;
    }

    [EditorTools.AberrationDescription("Script for helping the ingame UI determine events.", "Jacob Cooper", "15/10/2021")]
    public class EndGameUI : MonoBehaviour
    {
        [SerializeField, EditorTools.AberrationRequired] private Animator _animator;
        [SerializeField, EditorTools.AberrationRequired] private EndGameContainerData[] _data;

        public IEnumerator EndGameOver()
        {
            yield return new WaitForSeconds(1.5f);

            _animator.SetBool("GameOver", false);
        }

        private void Start()
        {
            _animator?.SetBool("GameOver", true);

            int winner = Round.Instance.GetWinner();
            foreach (var player in Base.Players.PlayerDictionary)
            {
                if (_data.Length > player.Value.playerID)
                {
                    EndGameContainerData data = _data[player.Value.playerID];

                    foreach (var shell in data.shell)
                        shell.material.color = player.Value.color;

                    data.container.SetActive(true);
                    data.score.text = $"Score: {player.Value.score}";
                    data.name.text = $"Player {player.Value.playerID + 1}";

                    if (winner == player.Value.playerID)
                    {
                        data.winContainer.SetActive(true);
                    }
                }
            }

            if (Round.Instance != null)
            {
                DestructionHeap.PrepareForDestruction(Round.Instance);
                Round.Instance = null;
            }

            Base.Players.PlayerDictionary = new Dictionary<int, Base.PlayerData>();

            DestructionHeap.PrepareForDestruction(Base.TickBase.Instance.gameObject);
            DestructionHeap.PrepareForDestruction(Settings.Instance.gameObject);
            DestructionHeap.PrepareForDestruction(DestructionHeap.Instance.gameObject);
        }
    }
}
