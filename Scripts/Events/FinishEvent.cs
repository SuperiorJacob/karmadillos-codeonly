using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Used by the UI, forcefully finish an event.", "Jacob Cooper", "14/11/2021")]
    public class FinishEvent : MonoBehaviour
    {
        public NumberDisplay display;
        public UnityEvent finishEvent;

        public void Start()
        {
            if (display != null)
                display.secondNumber = Base.Players.PlayerDictionary.Count;
        }

        public void Click()
        {
            if (display == null)
            {
                finishEvent?.Invoke();

                return;
            }

            display.number += 1;

            if (display.number >= display.secondNumber)
            {
                GameUILoad.Instance.SetTimerTime(Time.realtimeSinceStartup + 1f);
            }
        }
    }
}
