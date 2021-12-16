using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    [EditorTools.AberrationDeclare]
    public class Settings : MonoBehaviour
    {
        public static Settings Instance;

        public Utils.SO_Settings settingsReference;
        public int targetFrameRate = 60;
        public bool useVSync = false;

        private bool _discordEnabled = false;

        void Awake()
        {
            Instance = this;

            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = useVSync ? 1 : 0;

            _discordEnabled = settingsReference.enableDiscordRPC;

            DontDestroyOnLoad(this);

            // Cursed
            if (_discordEnabled)
                Utils.RichPresenceHandler.Init();
        }

        private void Update()
        {
            if (_discordEnabled && Utils.RichPresenceHandler.Handle != null)
                Utils.RichPresenceHandler.Handle.RunCallbacks();
        }

        private void OnDestroy()
        {
            if (_discordEnabled)
                Utils.RichPresenceHandler.Stop();

            //Destroy(Instance);
            //Instance = null;
        }

    }
}
