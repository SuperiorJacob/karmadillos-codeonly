using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    public class ModifiersCategorySwap : MonoBehaviour
    {
        private Animator anim;

        private void Start()
        {
            anim = this.GetComponent<Animator>();
        }

        public void Players()
        {
            anim.SetTrigger("Players");
            anim.ResetTrigger("Traps");
            anim.ResetTrigger("Level");
        }

        public void Traps()
        {
            anim.ResetTrigger("Players");
            anim.SetTrigger("Traps");
            anim.ResetTrigger("Level");
        }

        public void Level()
        {
            anim.ResetTrigger("Players");
            anim.ResetTrigger("Traps");
            anim.SetTrigger("Level");
        }
    }
}
