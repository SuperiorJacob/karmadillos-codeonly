using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Utils
{
    public class PlayerAudio : MonoBehaviour
    {
        [System.Serializable]
        public struct PAudio
        {
            public int clipID;
            public AudioClip[] audioClip;
        }

        /// <summary>
        /// Public list for trap audio struct objects
        /// </summary>
        public List<PAudio> clip = new List<PAudio>();
        public AudioSource audioSource;


        public void PlaySound(int a_id)
        {
            foreach (var clip in clip)
            {
                if (clip.clipID == a_id)
                {
                    if (!audioSource.isPlaying)
                    {
                       for (int i = 0; i< clip.audioClip.Length; i++)
                       {
                            audioSource.clip = clip.audioClip[i];
                            audioSource.Play();
                       }
                    }
                }
            }

        }

        /// <summary>
        /// Stops the current playing clip and sets it to null
        /// </summary>
        /// <param name="a_id"></param>
        public void StopSound(int a_id)
        {
            foreach (var clip in clip)
            {
                if (clip.clipID == a_id)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }
            }
        }
    }
}
