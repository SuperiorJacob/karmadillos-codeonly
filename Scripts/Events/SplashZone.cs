using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Trigger player splashing when triger is entered.", "Jacob Cooper", "14/11/2021")]
    public class SplashZone : MonoBehaviour
    {
        public void OnTriggerEnter(Collider a_other)
        {
            if (a_other.tag == "User" && a_other.gameObject.TryGetComponent(out Base.PlayerBase player))
            {
                player.Splash();
            }
        }
    }
}
