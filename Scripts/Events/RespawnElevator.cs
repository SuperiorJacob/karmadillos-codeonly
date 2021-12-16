using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("High Mobility respawn zone.", "Jacob Cooper", "15/10/2021")]
    public class RespawnElevator : MonoBehaviour
    {
        public Transform spawn;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "User")
            {
                collision.transform.position = spawn.position;
                collision.gameObject.transform.parent = null;

                if (collision.gameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.velocity = Vector3.zero;
                }
            }
        }
    }
}
