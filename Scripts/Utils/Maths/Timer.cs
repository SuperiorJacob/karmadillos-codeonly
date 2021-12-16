using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Utils.Maths
{
    [EditorTools.AberrationDescription("Timer class for making simple timers.", "Duncan Sykes", "14/11/2021")]
    public class Timer : MonoBehaviour
    {
        public void StartTimer(float seconds)
        {
           StartCoroutine(CountDown(seconds));
        }

        private IEnumerator CountDown(float time)
        {
            yield return new WaitForSeconds(time);
        }


    
    }
}
