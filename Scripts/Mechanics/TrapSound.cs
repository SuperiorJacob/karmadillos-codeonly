using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Structs
{   
    [RequireComponent(typeof(AudioSource)), 
        EditorTools.AberrationDescription("Handy script for running trap sounds.", "Duncan Sykes", "14/11/2021")]
    public class TrapSound : MonoBehaviour
    {
        /// <summary>
        /// Struct that contains each audio clip and its ID
        /// </summary>
        [System.Serializable]
        public struct TrapAudio
        {
            public int clipID;
            public AudioClip audioClip;
        }

        /// <summary>
        /// Public list for trap audio struct objects
        /// </summary>
        public List<TrapAudio> trapAudio = new List<TrapAudio>();
        public AudioSource audioSource;


        /// <summary>
        /// Plays the given clip via an ID reference based on the Trap Audio List
        /// </summary>
        /// <param name="a_id"></param>
        public void PlaySound(int a_id)
        {
            foreach(var clip in trapAudio)
            {
                if (clip.clipID == a_id)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.clip = clip.audioClip;
                        audioSource.Play();
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
            foreach (var clip in trapAudio)
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
