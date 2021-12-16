using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    [RequireComponent(typeof(Camera)),
        EditorTools.AberrationDescription("Simple camera replicator for UI camera stacks.", "Jacob Cooper", "4/11/2021")]
    public class CameraReplicator : MonoBehaviour
    {
        private Camera _thisCamera;

        private void Start()
        {
            _thisCamera = GetComponent<Camera>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_thisCamera == null || Camera.main == null)
                return;

            _thisCamera.fieldOfView = Camera.main.fieldOfView;
            _thisCamera.farClipPlane = Camera.main.farClipPlane;
            _thisCamera.nearClipPlane = Camera.main.nearClipPlane;
            _thisCamera.orthographic = Camera.main.orthographic;
            _thisCamera.orthographicSize = Camera.main.orthographicSize;
        }
    }
}
