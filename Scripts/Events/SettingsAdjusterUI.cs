using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("UI Interactor event mainly for changing settings.",
    "Jacob Cooper", "23/10/2021"), EditorTools.AberrationDeclare]
    public class SettingsAdjusterUI : MonoBehaviour
    {
        public string settingName;
        public bool updateSettingOnChange = true;

        [EditorTools.AberrationButton]
        public bool isNotToggle = true;

        public float defaultValue;
        public int minValue = 0;
        public int maxValue = 100;
        public int incrementAmount = 5;

        public string suffix;
        public string prefix;

        public float textChangeLerp = 10f;

        public TMP_Text text;

        [EditorTools.AberrationButton]
        public bool isDropdown = false;
        public TMP_Dropdown dropdown;
        [EditorTools.AberrationFinishButton]

        [EditorTools.AberrationButton]

        public bool hasAdjuster = false;
        public bool multiAdjust = false;
        public GameObject[] adjustables;

        [EditorTools.AberrationFinishButton]
        [EditorTools.AberrationFinishButton]

        private float _currentValue = 0;
        private float _goal = 0;

        public void Start()
        {
            if (Settings.Instance.settingsReference == null) return;

            if (!isNotToggle)
                return;

            object val = Settings.Instance.settingsReference.GetSetting(settingName);

            _currentValue = val != null ? (float)val : defaultValue;
            _goal = _currentValue;

            if (isDropdown)
                Settings.Instance.settingsReference.SetSettingReference(settingName, dropdown);

            ValueChanged();
        }

        public void Update()
        {
            if (!isNotToggle || isDropdown)
                return;

            if (textChangeLerp != 0)
                _currentValue = (int)Mathf.Lerp(_currentValue, _goal, Time.deltaTime * textChangeLerp);
            else
                _currentValue = _goal;

            text.text = $"{prefix}{_currentValue}{suffix}";
        }

        public void SettingsUpdate()
        {
            if (isNotToggle)
                Settings.Instance.settingsReference.SetSetting(settingName, _goal);
            else
                Settings.Instance.settingsReference.SetSetting(settingName, System.Convert.ToBoolean(_goal));
        }

        public void ValueChanged()
        {
            if (isDropdown)
            {
                dropdown.value = (int)_goal;
                //text.text = dropdown.options[_goal].text;
            }

            if (hasAdjuster)
            {
                HideAllAdjustables();

                int count = (int)(_goal / incrementAmount);

                if (!multiAdjust && count < adjustables.Length)
                    adjustables[count].SetActive(true);

                else if (multiAdjust)
                    for (int i = 0; i < count; i++)
                    {
                        adjustables[i].SetActive(true);
                    }
            }

            if (updateSettingOnChange)
                SettingsUpdate();
        }

        public void HideAllAdjustables()
        {
            foreach (var item in adjustables)
            {
                item.SetActive(false);
            }
        }

        public void Increment()
        {
            _goal += incrementAmount;

            if (_goal >= maxValue)
                _goal = maxValue;

            ValueChanged();
        }

        public void Toggle(bool a_toggle)
        {
            _goal = System.Convert.ToSingle(a_toggle);

            if (updateSettingOnChange)
                Settings.Instance.settingsReference.SetSetting(settingName, a_toggle);
        }

        public void Decrease()
        {
            _goal -= incrementAmount;

            if (_goal <= minValue)
                _goal = minValue;

            ValueChanged();
        }
    }
}
