using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    public class TitleToggle : MonoBehaviour
    {
        public float time;
        public GameObject animatedTitle;
        public GameObject staticTitle;

        void Start()
        {
            animatedTitle.SetActive(true);
            staticTitle.SetActive(false);
            Invoke("ToggleTitle", time);
        }

        void ToggleTitle()
        {
            animatedTitle.SetActive(false);
            staticTitle.SetActive(true);
        }
    }
}
