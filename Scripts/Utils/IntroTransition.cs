using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AberrationGames.Utils
{
    [EditorTools.AberrationDescription("Transition the intro to the main scene.", "Jacob Cooper", "14/11/2021")]
    public class IntroTransition : MonoBehaviour
    {
        public float transitionTime;

        void Start()
        {
            Invoke("TransitionScene", transitionTime);
        }

        void TransitionScene()
        {
            SceneManager.LoadScene(1);
        }
    }
}
