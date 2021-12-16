using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Respawn Zone brings the player back to the spawn position.", "Jacob Cooper", "15/10/2021")]
    public class Respawn : MonoBehaviour
    {
        public Transform spawn;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "User")
            {
                collision.transform.position = spawn.position;

                if (collision.gameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.velocity = Vector3.zero;
                }
            }
        }
    }
}
