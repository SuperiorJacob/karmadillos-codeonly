using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    public class SettingsCategorySwap : MonoBehaviour
    {
        private Animator anim;

        private void Start()
        {
            anim = this.GetComponent<Animator>();
        }

        public void Gameplay()
        {
            anim.SetTrigger("Gameplay");
            anim.ResetTrigger("Graphics");
            anim.ResetTrigger("Controls");
        }

        public void Graphics()
        {
            anim.ResetTrigger("Gameplay");
            anim.SetTrigger("Graphics");
            anim.ResetTrigger("Controls");
        }

        public void Controls()
        {
            anim.ResetTrigger("Gameplay");
            anim.ResetTrigger("Graphics");
            anim.SetTrigger("Controls");
        }
    }
}
