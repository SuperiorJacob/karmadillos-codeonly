using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Map voting for users on the main menu.", "Jacob Cooper", "14/11/2021")]
    public class MapVote : MonoBehaviour
    {
        [SerializeField] private float _timer;
        [SerializeField] private float _closeTimer;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private NumberDisplay _numbDisplay;

        [SerializeField] private VotedMap[] _maps;

        private float _currentTimer;

        private Dictionary<int, int> _mapSelection;

        public void PickMap()
        {
            Dictionary<int, int> mapVotes = new Dictionary<int, int>();

            foreach (var ply in _mapSelection)
            {
                if (!mapVotes.ContainsKey(ply.Value))
                    mapVotes[ply.Value] = 0;

                mapVotes[ply.Value]++;
            }

            int highest = 0;
            int index = 0;

            foreach (var map in mapVotes)
            {
                if (map.Value > highest)
                {
                    index = map.Key;
                    highest = map.Value;
                }
            }

            _maps[index].Goto();
        }

        public void SelectMap(int a_player, int a_map)
        {
            _mapSelection[a_player] = a_map;

            _numbDisplay.number = _mapSelection.Count;

            if (_numbDisplay.number >= _numbDisplay.secondNumber && (_currentTimer - Time.realtimeSinceStartup) > _closeTimer)
            {
                _currentTimer = Time.realtimeSinceStartup + _closeTimer;
            }

            foreach (var map in _maps)
            {
                map.icons[a_player].gameObject.SetActive(false);
            }

            _maps[a_map].icons[a_player].color = Base.Players.PlayerDictionary[a_player].color;
            _maps[a_map].icons[a_player].gameObject.SetActive(true);
        }

        private void Awake()
        {
            _mapSelection = new Dictionary<int, int>();
        }

        private void OnEnable()
        {
            _currentTimer = (Time.realtimeSinceStartup + _timer);

            for (int i = 0; i < _maps.Length; i++)
            {
                _maps[i].backReference = this;
                _maps[i].index = i;
            }
        }

        private void Update()
        {
            _text.text = "" + (int)(_currentTimer - Time.realtimeSinceStartup);

            if (_currentTimer < Time.realtimeSinceStartup)
            {
                bool failed = false;
                foreach (var player in Base.Players.PlayerDictionary)
                {
                    failed = player.Value.color == Color.white;
                    if (failed)
                        break;
                }

                if (failed)
                {
                    _currentTimer = Time.realtimeSinceStartup + _timer;

                    return;
                }

                _text.text = "0";

                PickMap();
            }

            _numbDisplay.secondNumber = Base.Players.PlayerDictionary.Count;
        }
    }
}
