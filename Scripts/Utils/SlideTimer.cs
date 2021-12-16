using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    public class SlideTimer : MonoBehaviour
    {
        public int timer;
        public Animator buttonAnimator;
        public Animator armadilloAnimator;
        public Animator footerAnimator;
        
        void Awake()
        {
            buttonAnimator.enabled = true;
            armadilloAnimator.enabled = true;
            footerAnimator.enabled = true;
            Invoke("ToggleAnimations", timer);
        }

        void ToggleAnimations()
        {
            buttonAnimator.enabled = false;
            armadilloAnimator.enabled = false;
            footerAnimator.enabled = false;
        }
    }
}
