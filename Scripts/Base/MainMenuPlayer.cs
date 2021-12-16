using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AberrationGames.Base
{
    [EditorTools.AberrationDescription("Script that turns a sprite into a on screen cursor that can interact with UI.", 
        "Jacob Cooper", "23/10/2021")]
    public class MainMenuPlayer : MonoBehaviour
    {
        public PlayerInput input;
        public Transform cursorPoint;
        public int playerIndex = 0;
        public AudioSource source;
        public AudioClip hoverSound;
        public AudioClip clickSound;

        [HideInInspector] public GraphicRaycaster raycaster;
        [HideInInspector] public InputDevice inputDevice;

        private Image _image;
        private Vector2 _moveDir;
        private PointerEventData _pointer;
        private List<RaycastResult> _results = new List<RaycastResult>();
        private Selectable _hover;
        private Selectable _lastHover;
        private bool _hovering = false;
        private bool _clicked = false;
        private Interfaces.IMenuInteraction _lastInteraction;

        public void BackPress(PlayerInput a_input, int a_playerID)
        {
            if (PlayerLoader.Instance != null)
                PlayerLoader.Instance.onBackPress.Invoke(a_input, a_playerID);
        }

        public void ClickPress(PlayerInput a_input, int a_playerID)
        {
            if (PlayerLoader.Instance != null && Players.PlayerDictionary[a_playerID].color != Color.white)
                PlayerLoader.Instance.onClickPress.Invoke(a_input, a_playerID);
        }

        public void SetPlayerIndex(int a_playerIndex)
        {
            playerIndex = a_playerIndex;
        }

        public PointerEventData GetPointer()
        {
            return _pointer;
        }

        public void Awake()
        {
            transform.localPosition = Vector2.zero;

            raycaster = transform.parent.GetComponent<GraphicRaycaster>();

            _image = GetComponent<Image>();
        }

        public void Start()
        {
            if (inputDevice != null)
                input.SwitchCurrentControlScheme(inputDevice);
        }

        public void Update()
        {
            if (Players.PlayerDictionary.ContainsKey(playerIndex))
                _image.color = Players.PlayerDictionary[playerIndex].color;

            _pointer = new PointerEventData(null);
            _pointer.position = cursorPoint.position;

            _results = new List<RaycastResult>();

            raycaster.Raycast(_pointer, _results);

            // Possibly needs an optimisation? But it is in the menu so.
            bool hovered = false;
            foreach (var result in _results)
            {
                if (result.gameObject.TryGetComponent(out Selectable button))
                {
                    _hover = button;
                    hovered = true;
                    button.OnPointerEnter(_pointer);
                }
            }

            if (!hovered && _hover != null)
            {
                _hover.OnPointerExit(_pointer);
                _hover = null;
                _hovering = false;
            }
            else if (hovered && _lastHover != null && _hover != _lastHover)
            {
                _lastHover.OnPointerExit(_pointer);
                _hovering = false;
            }
            else _hovering = true;

            _lastHover = _hover;

            // dahell
            if (Networking.Client.ConnectionManager.Instance != null)
                _image.color = Settings.Instance.settingsReference.playerDataReference.players[Networking.Client.ConnectionManager.Instance.playerData.playerID].color;
        }

        public void FixedUpdate()
        {
            float width = Screen.width;
            float height = Screen.height;

            float cursorSpeed = Settings.Instance.settingsReference.cursorSpeed * (width / 1000);

            transform.position += (Vector3)_moveDir * Time.fixedDeltaTime * cursorSpeed;

            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, 0, width),
                Mathf.Clamp(transform.position.y, 0, height),
                0
            );
        }

        public void Move(InputAction.CallbackContext a_context)
        {
            _moveDir = a_context.ReadValue<Vector2>();

            // Test
            //if (_moveDir.y > 0 || _moveDir.x > 0)
            //    UITraversing.Instance.GoPrev();
            //else
            //    UITraversing.Instance.GoNext();
        }

        public void Reload(InputAction.CallbackContext a_context)
        {
            if (a_context.started && _lastInteraction != null && gameObject.scene.IsValid())
            {
                _lastInteraction.ReloadClick(this, a_context.ReadValue<float>());
            }
        }

        public void Back(InputAction.CallbackContext a_context)
        {
            if (a_context.started && gameObject.scene.IsValid())
            {
                BackPress(input, playerIndex);
            }
        }

        public void Click(InputAction.CallbackContext a_context)
        {
            // Prefabs cause a weird ass bug that requires verification otherwise inputs are called twice.
            if (!gameObject.scene.IsValid())
                return;

            if (a_context.started && !_clicked)
            {
                // Test
                //UITraversing.Instance.Click(_pointer);

                _clicked = true;
                if (_hover != null && _hovering)
                {
                    if (!_hover.interactable)
                        ClickPress(input, playerIndex);

                    //_hover.OnPointerDown(_pointer);
                    if (_hover is Toggle)
                    {
                        Toggle hoverToggle = (Toggle)_hover;

                        hoverToggle.isOn = !hoverToggle.isOn;
                    }
                    else if (_hover is TMPro.TMP_Dropdown)
                    {
                        _hover.OnPointerDown(_pointer);
                    }
                    else if (_hover is Slider)
                    {
                        // Work on this
                        _hover.OnPointerDown(_pointer);
                    }
                    else
                    {
                        //ClickPress(input, playerIndex);
                        _hover.SendMessage("Press");
                    }

                    _lastInteraction = null;

                    // TODO
                    // Find a work around for this shit. Probs just make a function or reference.
                    if (_hover.TryGetComponent(out _lastInteraction))
                        _lastInteraction.SetUIClick(this);

                    if (_hover.TryGetComponent(out Events.FinishEvent finish))
                    {
                        finish.Click();
                        DestructionHeap.PrepareForDestruction(gameObject);
                    }
                }
                else
                {
                    ClickPress(input, playerIndex);

                    if (_lastInteraction != null)
                    {
                        _lastInteraction.SecondUIClick(this);
                    }
                }
            }
            else if (a_context.canceled && _clicked && !(_hover is TMPro.TMP_Dropdown))
            {
                _clicked = false;

                if (_hovering && _hover != null)
                {
                    _hover.OnPointerUp(_pointer);

                    if (_lastInteraction != null)
                    {
                        _lastInteraction.ReleaseUIClick(this);
                    }
                }
                else if (_lastHover != null)
                {
                    _lastHover.OnPointerUp(_pointer);

                    if (_lastInteraction != null)
                    {
                        _lastInteraction.ReleaseUIClick(this);
                    }
                }
            }
        }
    }
}
