using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;

namespace AberrationGames.Utils
{
    [System.Serializable]
    public enum SOSettingTypes
    {
        Float,
        Int,
        String,
        Char,
        Boolean,
        AudioVolume,
        AudioMute,
        DropDown,
        Object
    }

    [System.Serializable]
    public struct SOSettingData
    {
        public string settingName;
        public SOSettingTypes settingType;
        public object value;
        public object reference;
    }

    [CreateAssetMenu(fileName = "Settings", menuName = "Karma-Dillo/Settings"), 
        EditorTools.AberrationDescription("Game Settings", "Jacob Cooper", "07/10/2021"),
        EditorTools.AberrationDeclare()]
    public class SO_Settings : ScriptableObject
    {
        [EditorTools.AberrationToolBar("References")]
       
        [EditorTools.AberrationRequired] public AudioMixer mixerReference;
        [EditorTools.AberrationRequired] public UniversalRenderPipelineAsset renderReference;
        [EditorTools.AberrationRequired] public SO_PlayerData playerDataReference;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Settings")]

        [Header("User Feel")]
        public float cursorSpeed = 5f;
        public float sceneFakeLoadDelay = 5f;
        [Range(-180, 180), Tooltip("A negative value will flip the rotation")] public float trapRotationAmount = 45f;

        [Header("Game")]
        public float buildPhaseTimer = 30;
        public float gamePhaseTimer = 60;

        [Header("Camera")]
        public float deathCameraShakeIntensity = 3f;
        public float cameraZoomOutBase = 60f;
        public Vector2 cameraZoomOutClamp = new Vector2(30, 100);
        public float buildModeOrthographicSize = 10f;

        [Header("Rounds")]
        public int maxRound = 3;
        public int maxScoreToWin = 3;

        [Header("Discord")]
        [EditorTools.AberrationButton]
        public bool enableDiscordRPC = true;
        public long discordRPCID = 0;
        public Utils.RPCSettings rpcSettings;
        [EditorTools.AberrationFinishButton]

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("UI Settings")]

        public SOSettingData[] settingData;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Modifiers")]

        public float timeScale = 1;

        private bool _audioMuted = false;

        public object GetSetting(string a_name)
        {
            object value = null;

            foreach (var data in settingData)
            {
                if (data.settingName == a_name)
                {
                    value = data.value;
                    break;
                }
            }

            return value;
        }

        public void UpdateEvent(SOSettingData a_data)
        {
            if (a_data.settingType == SOSettingTypes.AudioVolume && !_audioMuted)
            {
                float val = System.Convert.ToSingle(a_data.value);
                val = -40 + ((val / 100) * 40);
                val = Mathf.Clamp(val, -40, 0);

                if (val == -40)
                    val = -80;

                mixerReference.SetFloat(a_data.settingName, val);
            }
            else if (a_data.settingType == SOSettingTypes.AudioMute)
            {
                bool val = (bool)a_data.value;
                _audioMuted = val;

                if (val)
                    mixerReference.SetFloat("Master Volume", -80);
                else
                    mixerReference.SetFloat("Master Volume", 0);
            }
            else if (a_data.settingType == SOSettingTypes.DropDown && a_data.reference != null)
            {
                // Subtley cursed.

                TMPro.TMP_Dropdown refer = (TMPro.TMP_Dropdown)a_data.reference;

                if (refer == null)
                    return;

                string str = refer.options[System.Convert.ToInt32(a_data.value)].text;

                if (a_data.settingName == "Screen Resolution")
                {
                    string[] split = str.Trim(' ').Split('x');

                    Screen.SetResolution(System.Convert.ToInt32(split[0]), System.Convert.ToInt32(split[1]), Screen.fullScreen);
                }
                else if (a_data.settingName == "Anti-Aliasing")
                {
                    int count = 1;
                    if (str == "2x")
                        count = 2;
                    else if (str == "4x")
                        count = 4;
                    else if (str == "8x")
                        count = 8;

                    renderReference.msaaSampleCount = count;
                }
                else if (a_data.settingName == "Shadow Resolution")
                {
                    //string split = str.Trim(' ');

                    //renderReference.mainLightShadowmapResolution = (UnityEngine.Rendering.Universal.ShadowResolution)(System.Int32.Parse(str));
                }
            }
            else if (a_data.settingType == SOSettingTypes.Boolean)
            {
                if (a_data.settingName == "Display Type")
                    Screen.fullScreen = System.Convert.ToBoolean(a_data.value);
                else if (a_data.settingName == "VSync")
                    QualitySettings.vSyncCount = System.Convert.ToInt32(a_data.value);
                else if (a_data.settingName == "HDR")
                    renderReference.supportsHDR = System.Convert.ToBoolean(a_data.value);
            }
            else if (a_data.settingType == SOSettingTypes.Float)
            {
                if (a_data.settingName == "Render Scale")
                    renderReference.renderScale = System.Convert.ToSingle(a_data.value);
            }
        }

        public void SetSetting(string a_name, object a_value)
        {
            for (int i = 0; i < settingData.Length; i++)
            {
                var data = settingData[i];

                if (data.settingName == a_name)
                {
                    data.value = a_value;
                    settingData[i] = data;

                    UpdateEvent(data);

                    break;
                }
            }
        }

        public void SetSettingReference(string a_name, object a_reference)
        {
            for (int i = 0; i < settingData.Length; i++)
            {
                var data = settingData[i];

                if (data.settingName == a_name)
                {
                    data.reference = a_reference;
                    settingData[i] = data;

                    break;
                }
            }
        }
    }
}
