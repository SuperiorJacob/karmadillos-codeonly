using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Trap sticker used for rotation & displaying a UI stick for traps.", "Jacob Cooper", "14/11/2021")]
    public class TrapSticker : MonoBehaviour
    {
        [HideInInspector] public GameObject mechanic;

        [SerializeField] private RectTransform _arrowTransform;
        [SerializeField] private Image _trapImage;

        private RectTransform _rectTransform;

        public void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void SetStickerColor(Color a_color)
        {
            _trapImage.color = a_color;
        }

        public void SetArrowActive(bool a_active)
        {
            _arrowTransform.gameObject.SetActive(a_active);
        }

        public void Rotate(float a_angle, Transform a_trap, float a_trapOffset = 0)
        {
            a_trap.Rotate(-Vector3.up, a_angle + a_trapOffset);

            _arrowTransform.rotation = Quaternion.Euler(_arrowTransform.eulerAngles.x,
                _arrowTransform.eulerAngles.y, _arrowTransform.eulerAngles.z + a_angle);
        }

        public void SetPositionRelative(Vector3 a_position)
        {
            if (_rectTransform == null)
                return;

            _rectTransform.position = Vector3.Lerp(_rectTransform.position, a_position, Time.deltaTime * 100f);
        }

        public void SetIcon(Sprite a_image)
        {
            _trapImage.sprite = a_image;
        }

        public void FixedUpdate()
        {
            if (mechanic == null)
                DestructionHeap.PrepareForDestruction(gameObject);
        }
    }
}
