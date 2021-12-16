using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Utils
{
    [EditorTools.AberrationDescription("Custom animation rigging on characters.", "Jacob Cooper", "15/10/2021")]
    public class BoneFollow : MonoBehaviour
    {
        public Camera renderCamera;
        public float horizontalSpeed = 50f;
        public float verticalSpeed = 50f;
        public float lerpSpeed = 10f;
        public Vector3 offset;

        private Vector3 _prevEuler;
        private bool _started = false;

        public void LateUpdate()
        {
            if (Base.Players.PlayerDictionary == null || Base.Players.PlayerDictionary.Count < 1 || Base.Players.PlayerDictionary[0].player == null)
                return;

            Vector3 pos = renderCamera.transform.TransformPoint(Base.Players.PlayerDictionary[0].player.transform.localPosition / 1200);

            if (!_started)
            {
                _started = true;
                _prevEuler = new Vector3(
                    offset.x,
                    (pos.x * horizontalSpeed) + offset.y,
                    (pos.y * verticalSpeed) + offset.z
                );
                transform.localEulerAngles = _prevEuler;
            }

            _prevEuler = new Vector3(
                offset.x, 
                Mathf.Lerp(_prevEuler.y, (pos.x * horizontalSpeed) + offset.y, Time.deltaTime * lerpSpeed),
                Mathf.Lerp(_prevEuler.z, (pos.y * verticalSpeed) + offset.z, Time.deltaTime * lerpSpeed)
            );

            transform.localEulerAngles = _prevEuler;
        }
    }
}
