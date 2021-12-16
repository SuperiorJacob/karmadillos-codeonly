using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Displaying numbers on UI.", "Jacob Cooper", "14/11/2021")]
    public class NumberDisplay : MonoBehaviour
    {
        public TMP_Text text;

        public int number;
        public int secondNumber;
        public string defaultText;

        void FixedUpdate()
        {
            text.text = defaultText.Replace("{x}", "" + number).Replace("{y}", "" + secondNumber);
        }
    }
}
