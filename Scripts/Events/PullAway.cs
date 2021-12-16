using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Sucks 'User' objects towards it.", "Jacob Cooper", "15/10/2021")]
    public class PullAway : MonoBehaviour
    {
        public float suckSpeed = 10f;

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.tag == "User")
            {
                Vector3 dir = (collision.gameObject.transform.position - transform.position).normalized;
                dir += -Vector3.up;
                dir = dir.normalized;

                if (Networking.NetworkPlayer.localClient != null)
                {
                    if (Networking.NetworkPlayer.localClient.gameObject == collision.gameObject)
                    {
                        Networking.NetworkPlayer.localClient.rb.AddForce(dir * suckSpeed * Time.fixedDeltaTime, ForceMode.Impulse);
                    }
                }
                else
                {
                    if (collision.rigidbody != null)
                    {
                        collision.rigidbody.AddForce(dir * suckSpeed * Time.fixedDeltaTime, ForceMode.Impulse);
                    }
                }
            }
        }
    }
}
