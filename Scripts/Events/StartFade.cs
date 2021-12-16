using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("UI Utility for fading after a specific time.", "Jacob Cooper", "15/10/2021")]
    public class StartFade : MonoBehaviour
    {
        [SerializeField] private Image _fadeUI;
        [SerializeField] private float _fadeTime;
        [SerializeField] private float _fadeAfter;

        private float _startTime;
        private float _endTime;
        private bool _started = false;

        public void Begin(Sprite a_sprite, Color a_color = default)
        {
            _startTime = Time.realtimeSinceStartup + _fadeAfter;
            _endTime = _startTime + _fadeTime;

            _started = true;

            _fadeUI.sprite = a_sprite;
            _fadeUI.color = a_color;

            _fadeUI.gameObject.SetActive(true);
        }

        public void JustFade(InputAction.CallbackContext a_context)
        {
            if (a_context.canceled)
            {
                _started = true;

                _fadeUI.color = new Color(_fadeUI.color.r, _fadeUI.color.g, _fadeUI.color.b, 1);
                _startTime = Time.realtimeSinceStartup + _fadeTime;
                _endTime = _startTime + _fadeTime;
            }
            else
            {
                _started = false;
                _fadeUI.color = new Color(_fadeUI.color.r, _fadeUI.color.g, _fadeUI.color.b, 1);
                _fadeUI.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (_started && Time.realtimeSinceStartup > _startTime)
            {
                _fadeUI.color = new Color(_fadeUI.color.r, _fadeUI.color.g, _fadeUI.color.b, ((_endTime - Time.realtimeSinceStartup) / _fadeTime));

                if (Time.realtimeSinceStartup > _endTime)
                {
                    _fadeUI.gameObject.SetActive(false);
                }
            }
        }
    }
}
