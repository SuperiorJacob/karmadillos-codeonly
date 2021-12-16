using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Anchors the object to a specific axis and point in space.", "Jacob Cooper", "15/10/2021")]
    public class AnchorPoint : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset;
        [SerializeField] private bool _faceCamera;

        private Transform _attachment;

        private void Start()
        {
            _attachment = transform.parent;
            transform.SetParent(null, false);
        }

        private void Update()
        {
            if (_attachment == null)
                return;

            Vector3 incre = new Vector3(0, _offset.y, 0) + _offset.z * _attachment.forward + _offset.x * _attachment.right;

            transform.position = _attachment.position + incre;

            if (_faceCamera)
                transform.LookAt(Camera.main.transform);
        }
    }
}
