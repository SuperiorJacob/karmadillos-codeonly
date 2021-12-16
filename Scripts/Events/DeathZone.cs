using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("When the player enters this area, kills them for the round.", "Jacob Cooper", "15/10/2021")]
    public class DeathZone : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "User" 
                && collision.gameObject.TryGetComponent(out Base.PlayerBase player))
            {
                Round.Instance.PlayerDeath(player.GetPlayerIndex(), player.LastPlayerHit() != null ? player.LastPlayerHit().GetPlayerIndex() : -1);
            }
        }
    }
}
