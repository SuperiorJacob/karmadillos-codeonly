using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AberrationGames.Utils
{
    [EditorTools.AberrationDescription("Simple class for having a count down timer.", "Jacob Cooper", "14/11/2021")]
    public class CountDownTimer : MonoBehaviour
    {
        public TMP_Text timer = null;

        private float _waitTime = 0;

        void Awake()
        {
            // To change
            _waitTime = Time.realtimeSinceStartup + Networking.Shared.NetworkInformation.WaitTime;
        }

        void FixedUpdate()
        {
            timer.text = $"{Mathf.Round(_waitTime - Time.realtimeSinceStartup)}s";
        }
    }
}
