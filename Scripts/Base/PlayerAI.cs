using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Base
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug), 
        EditorTools.AberrationDescription("Designed to provide realtime physics iteractions to the player", "Jacob Cooper", "22/10/2021"),
        RequireComponent(typeof(PlayerBase))]
    public class PlayerAI : MonoBehaviour
    {
        [SerializeField] private int _playerID;

        private PlayerBase _player;
        private GameObject _target;
        private bool _initialized = false;

        private void Start()
        {
            _player = GetComponent<PlayerBase>();
        }

        private void FindTarget()
        {
            GameObject target = null;

            foreach (var ply in Base.Players.GetActivePlayers())
            {
                if (ply.player != null && ply.player != gameObject)
                {
                    target = ply.player;

                    break;
                }
            }

            _target = target;
        }

        private void FixedUpdate()
        {
            FindTarget();
        }

        private void Update()
        {
            if (_target == null)
                return;
            else if (!_initialized)
            {
                _initialized = true;

                Players.PlayerDictionary[_playerID] = new PlayerData
                {
                    id = _playerID,
                    player = gameObject,
                    input = { },
                    device = { },
                    playerID = _playerID,
                    score = 0
                };

                _player.SetPlayerIndex(_playerID);
            }

            Vector3 dir = (_target.transform.position - transform.position).normalized;

            _player.Movement(dir.z, dir.x);
        }
    }
}
