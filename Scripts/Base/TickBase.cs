using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Base
{
    public abstract class AberrationMonoBehaviour : MonoBehaviour 
    {
        public virtual void Start()
        {
            TickBase.UpdateTick += UpdateTick;
        }

        public virtual void OnDestroy()
        {
            TickBase.UpdateTick -= UpdateTick;
        }

        public virtual void UpdateTick(float a_tickDelta)
        {

        }
    }

    [RequireComponent(typeof(Settings)),
        EditorTools.AberrationDescription("A tick system to control physics and update without modifying time scale.", "Jacob Cooper", "14/11/2021")]
    public class TickBase : MonoBehaviour
    {
        public static TickBase Instance;

        public static float LocalTimeScale = 1f;

        public static float deltaTime
        {
            get
            {
                return Time.deltaTime * LocalTimeScale;
            }
        }

        public static float fixedDeltaTime
        {
            get
            {
                return Time.fixedDeltaTime * LocalTimeScale;
            }
        }

        public static float timeScale
        {
            get 
            { 
                return Time.timeScale * LocalTimeScale; 
            }
        }

        public static float GetTimeBasedOnScale(float a_timer)
        {
            if (LocalTimeScale < 1f)
                a_timer *= 1 + LocalTimeScale;
            else
                a_timer /= LocalTimeScale;

            return a_timer;
        }

        public static Action<float> UpdateTick;

        public static float BaseScale = 1f;
        
        private float _timer;

        public static bool IsPaused()
        {
            return LocalTimeScale == 0f;
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);

                return;
            }

            LocalTimeScale = BaseScale;

            DontDestroyOnLoad(gameObject);

            Instance = this;
        }

        private void Start()
        {
            Physics.autoSimulation = false;

            LocalTimeScale = Settings.Instance.settingsReference.timeScale;
        }

        private void Update()
        {
            if (Physics.autoSimulation)
                return;

            _timer += deltaTime;

            while (!IsPaused() && _timer >= fixedDeltaTime)
            {
                _timer -= fixedDeltaTime;
                Physics.Simulate(fixedDeltaTime);

                UpdateTick?.Invoke(fixedDeltaTime);
            }
        }
    }
}
