using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    // TODO
    // List players so anyone can spawn this trap instead of one at a time.

    [EditorTools.AberrationDescription("Simple UI functionality that allows the player to move traps into the scene.", "Jacob Cooper", "15/10/2021")]
    public class PlaceTrap : MonoBehaviour, Interfaces.IMenuInteraction
    {
        public AssetReference trapPrefab;
        public AssetReference trapIconPrefab;
        public Selectable trapButton;
        public Sprite trapIcon;
        public LayerMask bluePrintLayer;
        public bool rotates = true;

        [SerializeField] private float _trapDistanceSize = 5f;
        [SerializeField] private float _trapOffset = 0f;
        [SerializeField] private FinishEvent _finishReference;

        private GameObject _currentTrap;
        private TrapSticker _currentTrapSticker;

        private Transform _trapParent;

        private Vector3 _trapPos;
        private Vector3 _rotation;

        private RaycastHit _oldHit = new RaycastHit();

        private Base.MainMenuPlayer _currentPlayer;
        private GameObject _loadedTrap;
        private GameObject _loadedTrapIcon;
        private bool _currentlyBad = false;

        public GameObject GetTrap()
        {
            return _currentTrap;
        }

        public bool HasPlayer()
        {
            return _currentPlayer != null;
        }

        public void OnEnable()
        {
            _currentPlayer = null;

            if (_currentTrap != null)
            {
                DestructionHeap.PrepareForDestruction(_currentTrap);
                DestructionHeap.PrepareForDestruction(_currentTrapSticker);
            }
        }

        public void Awake()
        {
            // Should probably use this idea everywhere else lmfao.
            Addressables.LoadAssetAsync<GameObject>(trapPrefab).Completed += handle => { 
                _loadedTrap = handle.Result;

                Addressables.LoadAssetAsync<GameObject>(trapIconPrefab).Completed += handle => {
                    _loadedTrapIcon = handle.Result;
                };
            };
        }

        public bool CheckTooClose()
        {
            foreach (var trap in GameUILoad.Instance.stickers)
            {
                if (trap == this || trap.mechanic == null)
                    continue;

                float distance = Vector3.Distance(trap.mechanic.transform.position, _currentTrap.transform.position);

                if (distance < _trapDistanceSize)
                    return true;
            }

            return false;
        }

        public void Update()
        {
            if (_currentTrap == null || _currentPlayer == null) return;

            (Ray ray, RaycastHit hit) = MoveTrap();
            
            _trapPos = hit.point;

            _rotation = hit.normal;

            _currentTrap.transform.position = _trapPos;

            _currentTrapSticker.SetPositionRelative(_currentPlayer.cursorPoint.position);

            _currentlyBad = (_trapParent == null || CheckTooClose());

            _currentTrapSticker.SetStickerColor(_currentlyBad ? Color.red : Color.white);
        }

        [EditorTools.AberrationDescription("Dragging the trap.", "Jacob Cooper", "23/09/2021")]
        public void SetUIClick(Base.MainMenuPlayer a_player)
        {
            if (_currentPlayer != null || !trapButton.interactable) 
                return;

            var player = Base.Players.PlayerDictionary[a_player.playerIndex];

            if (player.dead)
                return;

            _currentPlayer = a_player;
            _currentTrap = Instantiate(_loadedTrap);
            _currentTrap.tag = "Trap";

            // Make trap invisible
            if (_currentTrap.TryGetComponent(out MeshRenderer render))
                render.enabled = false;

            foreach (var renders in _currentTrap.GetComponentsInChildren<MeshRenderer>())
            {
                renders.enabled = false;
            }
            //

            Instantiate(_loadedTrapIcon).TryGetComponent(out _currentTrapSticker);
            _currentTrapSticker.mechanic = _currentTrap;
            _currentTrapSticker.tag = "Trap";
            _currentTrapSticker.transform.SetParent(transform.parent.parent);
            _currentTrapSticker.SetIcon(trapIcon);

            _currentTrapSticker.SetArrowActive(rotates);

            trapButton.interactable = false;

            // Set the player as dead so that he can't select anymore.
            SetDead(a_player, true);
        }

        public void ReloadClick(Base.MainMenuPlayer a_player, float a_reloadAmount = 1f)
        {
            if (a_player != _currentPlayer) return;

            if (_trapParent != null && _currentTrapSticker != null && rotates)
            {
                // Goes right :)
                _currentTrapSticker.Rotate(a_reloadAmount * -Settings.Instance.settingsReference.trapRotationAmount, _currentTrap.transform, _trapOffset);
            }
        }

        public void ReleaseUIClick(Base.MainMenuPlayer a_player)
        {

        }

        public void SetDead(MainMenuPlayer a_player, bool a_dead)
        {
            var player = Base.Players.PlayerDictionary[a_player.playerIndex];
            player.dead = a_dead;

            Base.Players.PlayerDictionary[a_player.playerIndex] = player;
        }

        [EditorTools.AberrationDescription("Placing the trap.", "Jacob Cooper", "23/09/2021")]
        public void SecondUIClick(MainMenuPlayer a_player)
        {
            if (a_player != _currentPlayer) return;

            if (!_currentlyBad)
            {
                _currentTrap.transform.position = _trapPos;

                _rotation += new Vector3(0, _currentTrap.transform.rotation.eulerAngles.y, 0);
                _currentTrap.transform.rotation = Quaternion.Euler(_rotation);

                _currentTrap.transform.SetParent(_trapParent);
                _currentTrapSticker.SetPositionRelative(_currentPlayer.cursorPoint.position);

                GameUILoad.Instance.stickers.Add(_currentTrapSticker);

                _finishReference.Click();
                DestructionHeap.PrepareForDestruction(a_player.gameObject);
            }
            else
            {
                DestructionHeap.PrepareForDestruction(_currentTrap);
                DestructionHeap.PrepareForDestruction(_currentTrapSticker.gameObject);

                _currentTrapSticker = null;
                trapButton.interactable = true;

                SetDead(a_player, false);
            }

            _currentTrap = null;
            _currentPlayer = null;
        }

        public void OnDestroy()
        {
            //if (_currentTrap != null && _currentTrap.transform.parent == null)
            //{
            //    Destroy(_currentTrap);
            //}
        }

        private void OnDisable()
        {
            //OnDestroy();
        }

        private (Ray ray, RaycastHit hit) MoveTrap()
        {
            if (_currentPlayer == null) return (new Ray(), _oldHit);

            Ray ray = Camera.main.ScreenPointToRay(_currentPlayer.GetPointer().position);

            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, bluePrintLayer))
            {
                _oldHit = hit;
                _trapParent = hit.transform;
            }
            else
            {
                _oldHit = new RaycastHit();
                _oldHit.point = ray.origin + ray.direction * 10;
                _trapParent = null;
            }

            return (ray, _oldHit);
        }
    }
}
