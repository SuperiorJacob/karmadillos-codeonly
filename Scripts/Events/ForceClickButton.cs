using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AberrationGames
{
    [EditorTools.AberrationDescription("Used by UI, force click a button.", "Jacob Cooper", "14/11/2021")]
    public class ForceClickButton : MonoBehaviour
    {
        public Button button;
        public bool requiresActivity = true;

        public void ClickByMaster(PlayerInput a_input, int a_playerID)
        {
            if (a_playerID == 0)
                Click();
        }

        public void Click()
        {
            if (requiresActivity && !gameObject.activeInHierarchy)
                return;

            button.onClick.Invoke();
        }
    }
}
