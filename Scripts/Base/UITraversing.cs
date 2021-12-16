using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AberrationGames.Base
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug),
        EditorTools.AberrationDescription("Traversing through different types of UI using input system.", "Jacob Cooper", "14/10/2021")]
    public class UITraversing : MonoBehaviour
    {
        public static UITraversing Instance;

        [EditorTools.AberrationButton(EditorTools.DeclaredButtonTypes.Button, false, "Traverse", "GoNext", null)]
        public Selectable[] traversable;

        public float nextMove = 0.1f;

        private int _current = 0;
        private float _nextChange = 0;

        public void SetCurrent(int a_num)
        {
            if (_nextChange > Time.realtimeSinceStartup)
                return;

            _current = a_num;

            _nextChange = Time.realtimeSinceStartup + nextMove;

            traversable[_current].Select();
        }

        public void GoNext()
        {
            int next = _current + 1;

            if (next > (traversable.Length - 1))
                next = 0;

            SetCurrent(next);
        }

        public void GoPrev()
        {
            int prev = _current - 1;

            if (prev < 0)
                prev = traversable.Length - 1;

            SetCurrent(prev);
        }

        public void Click(PointerEventData a_pointer)
        {

            if (traversable[_current] is Button)
            {
                Button button = (Button)traversable[_current];

                button.OnPointerClick(a_pointer);
            }
        }

        public void SetInstance()
        {
            Instance = this;

            SetCurrent(0);
        }

        public void Start()
        {
            //SetInstance();
        }

        private void OnDestroy()
        {
            Destroy(Instance);
            Instance = null;
        }
    }
}
