using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    [System.Serializable]
    public struct IngameUIData
    {
        public GameObject container;
        public Image playerBacker;
        public Image stunnedPlayer;
        public Image deadPlayer;
        public Image icon;
        public TMP_Text score;

        [Header("Break down")]
        public GameObject breakdownContainer;
        public Image breakdownIcon;
        public Image breakdownBacker;
        public GameObject scoreContainer;
    }

    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug),
        EditorTools.AberrationDescription("Loading the in game UI.", "Jacob Cooper", "23/09/2021")]
    public class GameUILoad : MonoBehaviour
    {
        public static GameUILoad Instance { private set; get; }

        public Animator animatorCountdown;

        [HideInInspector] public List<TrapSticker> stickers;

        [SerializeField] private float _updateSpeed = 5f;
        [SerializeField] private Base.PlayerLoader _playerLoader;
        [SerializeField] private NumberDisplay _display;
        [SerializeField] private GameObject _container;
        [SerializeField] private GameObject _roundBreakdown;
        [SerializeField] private GameObject _trapsSpawning;
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private GameObject _disconnectedText;
        [SerializeField] private TMP_Text _timer;
        [SerializeField] private TMP_Text _roundBreakdownName;

        public List<PlaceTrap> traps;
        [SerializeField] private int _randomizeAmount;
        [SerializeField, EditorTools.AberrationRequired()] private IngameUIData[] _ingameData;

        private bool _trapUISet = false;
        private float _currentTimer = 0;
        private bool _timing = false;
        private string _timerText = "";
        private bool _pauseState = false;
        private float _resumeTime = 0f;

        private List<GameObject> _cachedCursorsPauseMenu;

        public void QuitToMainMenu()
        {
            if (Round.Instance != null)
            {
                DestructionHeap.PrepareForDestruction(Round.Instance);
                Round.Instance = null;
            }

            Base.Players.PlayerDictionary = new Dictionary<int, Base.PlayerData>();

            DestructionHeap.PrepareForDestruction(Base.TickBase.Instance.gameObject);
            DestructionHeap.PrepareForDestruction(Settings.Instance.gameObject);
            DestructionHeap.PrepareForDestruction(DestructionHeap.Instance.gameObject);

            SceneManager.LoadScene(1);
        }

        public void PauseMenu(bool a_on, bool a_disconnected = false)
        {
            _pauseMenu.SetActive(a_on);
            _disconnectedText.SetActive(a_disconnected);

            Base.TickBase.LocalTimeScale = a_on ? 0 : Base.TickBase.BaseScale;

            // Get rid of any cursors in game.
            if (Base.Players.PlayerDictionary.Count < 2)
            {
                if (a_on && !a_disconnected)
                    PauseMenu(false);

                return;
            }

            // Creating and loading cursors externally.

            if (!a_on)
            {
                // Remake cursors / remove them.
                if (_cachedCursorsPauseMenu != null)
                    foreach (var ply in _cachedCursorsPauseMenu)
                        DestructionHeap.PrepareForDestruction(ply);

                _cachedCursorsPauseMenu = null;
            }
            else
            {
                if (_cachedCursorsPauseMenu == null)
                    _cachedCursorsPauseMenu = new List<GameObject>();
                else
                {
                    foreach (var ply in _cachedCursorsPauseMenu)
                        DestructionHeap.PrepareForDestruction(ply);
                }

                // Load cursors
                for (int i = 0; i < Base.Players.PlayerDictionary.Count; i++)
                {
                    Base.PlayerData player = Base.Players.PlayerDictionary[i];
                    if (player.disconnected)
                        continue;

                    Addressables.InstantiateAsync(_playerLoader.playerPrefab, _playerLoader.canvas.position, default, _playerLoader.canvas, true).Completed += handle =>
                    {
                        _cachedCursorsPauseMenu.Add(_playerLoader.LoadPlayer(handle, player).player);
                    };
                }
            }
        }

        public void UnPause()
        {
            PauseMenu(false);
        }

        public void RandomizeTraps()
        {
            foreach (var trap in traps)
            {
                trap.gameObject.SetActive(false);
            }

            // Wacky, should just shuffle and pick top 4, using this for now.
            foreach (var trap in traps.OrderBy(f => System.Guid.NewGuid()).Distinct().Take(_randomizeAmount))
            {
                trap.gameObject.SetActive(true);
            }
        }

        public int ConvertGameObjectToID(GameObject a_object)
        {
            foreach (var player in Base.Players.GetActivePlayers())
            {
                if (player.player == a_object)
                {
                    return player.playerID;
                }
            }

            return -1;
        }

        [EditorTools.AberrationDescription("Player charge display.", "Jacob Cooper", "23/09/2021")]
        public void SetCharge(int a_id, float a_charge)
        {
            //_ingameData[a_id].charge.value = Mathf.Lerp(_ingameData[a_id].charge.value, a_charge, Time.deltaTime * _updateSpeed);
        }

        // Temporary
        public void SetCharge(GameObject a_player, float a_charge)
        {
            int id = ConvertGameObjectToID(a_player);
            if (id != -1)
                SetCharge(id, a_charge);
        }

        [EditorTools.AberrationDescription("Player score display.", "Jacob Cooper", "12/11/2021")]
        public void SetScore(int a_id, int a_score)
        {
            _ingameData[a_id].score.text = $"Score: {a_score}";
        }

        // Prototype
        [EditorTools.AberrationDescription("Knock threshold setter.", "Jacob Cooper", "3/11/2021")]
        public void SetKnockThreshold(int a_id, float a_threshold)
        {
            //_ingameData[a_id].knockThreshold.value = Mathf.Lerp(_ingameData[a_id].knockThreshold.value, a_threshold, Time.deltaTime * _updateSpeed);
        }

        [EditorTools.AberrationDescription("Set if a player is alive or not on the UI.", "Jacob Cooper", "07/10/2021")]
        public void SetDead(int a_id, bool a_dead)
        {
            IngameUIData data = _ingameData[a_id];

            if (a_dead)
            {
                data.deadPlayer.gameObject.SetActive(true);
                data.playerBacker.gameObject.SetActive(false);
                data.stunnedPlayer.gameObject.SetActive(false);

                if (Base.PlayerLoader.Instance != null && Base.PlayerLoader.Instance.noiseCam != null)
                {
                    Base.PlayerLoader.Instance.noiseCam.m_AmplitudeGain = Settings.Instance.settingsReference.deathCameraShakeIntensity;
                }
            }
            else
            {
                data.deadPlayer.gameObject.SetActive(false);
                data.playerBacker.gameObject.SetActive(true);
                data.stunnedPlayer.gameObject.SetActive(false);
            }
        }

        [EditorTools.AberrationDescription("Enables the stun UI for the player.", "Jacob Cooper", "3/11/2021")]
        public void SetStunned(int a_id, bool a_stunned)
        {
            IngameUIData data = _ingameData[a_id];

            if (a_stunned)
            {
                data.deadPlayer.gameObject.SetActive(false);
                //data.playerBacker.SetActive(false);
                data.stunnedPlayer.gameObject.SetActive(true);
            }
            else
            {
                data.deadPlayer.gameObject.SetActive(false);
                //data.playerBacker.SetActive(true);
                data.stunnedPlayer.gameObject.SetActive(false);
            }
        }

        [EditorTools.AberrationDescription("Enable the breakdown UI.", "Jacob Cooper", "12/11/2021")]
        public void EnableBreakdown(bool a_bool)
        {
            _roundBreakdown.SetActive(a_bool);

            if (a_bool)
            {
                _roundBreakdownName.text = $"ROUND {Round.Instance.GetRound()}";

                for (int i = 0; i < _ingameData.Length; i++)
                {
                    var breakdown = _ingameData[i];

                    if ((Base.Players.PlayerDictionary.Count - 1) < i)
                    {
                        breakdown.breakdownContainer.SetActive(false);
                        continue;
                    }
                    else
                        breakdown.breakdownContainer.SetActive(true);

                    int count = breakdown.scoreContainer.transform.childCount;
                    int score = Base.Players.PlayerDictionary[i].score;

                    Transform child = breakdown.scoreContainer.transform.GetChild(0);
                    if (score > 0)
                        child.gameObject.SetActive(true);

                    if (count < score)
                    {
                        for (int ii = 0; ii < (score - count); ii++)
                        {
                            // A lil cursed
                            Instantiate(child.gameObject, breakdown.scoreContainer.transform);
                        }
                    }
                }
            }
        }

        // Temporary
        public void SetScore(GameObject a_player, int a_score)
        {
            int id = ConvertGameObjectToID(a_player);
            if (id != -1)
                SetScore(a_player, a_score);
        }

        [EditorTools.AberrationDescription("Set the timers time.", "Jacob Cooper", "3/11/2021")]
        public void SetTimerTime(float a_timerTime)
        {
            _currentTimer = a_timerTime;

            _timing = true;
        }

        [EditorTools.AberrationDescription("Set trap spawning enabled or disabled.", "Jacob Cooper", "23/09/2021")]
        public void SetTrapUI(bool a_set)
        {
            if (_trapUISet == a_set)
                return;

            //foreach (var trap in traps)
            //{
            //    // Make sure the UI refuses to go if a player hasn't placed a trap.
            //    if (_currentTimer > Time.realtimeSinceStartup && _trapUISet)
            //    {
            //        if (trap.HasPlayer())
            //            return;
            //    }
            //}

            foreach (var trap in traps)
            {
                if (trap.GetTrap() != null)
                    DestructionHeap.PrepareForDestruction(trap.GetTrap());
            }

            _timing = false;

            _trapUISet = a_set;

            _trapsSpawning.SetActive(a_set);
            _container.SetActive(!a_set);

            if (!a_set)
            {
                foreach (var player in Base.Players.GetActivePlayers())
                {
                    if (player.player != null)
                        DestructionHeap.DestructPlayer(player.playerID);
                }

                // Ingame Traps
                foreach (var trap in GameObject.FindGameObjectsWithTag("Trap"))
                {
                    if (trap.TryGetComponent(out MeshRenderer render))
                        render.enabled = true;

                    foreach (var renders in trap.GetComponentsInChildren<MeshRenderer>())
                    {
                        renders.enabled = true;
                    }
                }

                Round.Instance.StartGame();
            }
            else
            {
                _playerLoader.TrapLoading();
                _display.number = 0;
                _display.secondNumber = Base.Players.PlayerDictionary.Count;

                // UI Trap
                foreach (var trap in traps)
                {
                    trap.trapButton.interactable = true;
                }

                // Ingame Traps
                foreach (var trap in GameObject.FindGameObjectsWithTag("Trap"))
                {
                    if (trap.TryGetComponent(out MeshRenderer render))
                        render.enabled = false;

                    foreach (var renders in trap.GetComponentsInChildren<MeshRenderer>())
                    {
                        renders.enabled = false;
                    }
                }

                SetTimerTime(Time.realtimeSinceStartup + Settings.Instance.settingsReference.buildPhaseTimer);
            }
        }

        [EditorTools.AberrationDescription("Setup the UI to reflect player information.", "Jacob Cooper", "23/09/2021")]
        public void Setup()
        {
            _container.SetActive(true);

            foreach (var player in Base.Players.GetActivePlayers())
            {
                if (_ingameData.Length > player.playerID)
                {
                    int id = player.playerID;
                    var data = _ingameData[id];
                    data.container.SetActive(true);
                    data.playerBacker.color = player.color;
                    data.deadPlayer.color = player.color;
                    data.stunnedPlayer.color = player.color;
                    data.icon.color = player.color;
                    data.breakdownIcon.color = player.color;
                    data.breakdownBacker.color = player.color;

                    SetCharge(id, 0);
                    SetScore(id, player.score);
                    SetDead(id, false);
                }
            }

            RandomizeTraps();
        }

        private void Start()
        {
            Instance = this;

            Setup();

            _timerText = _timer.text;
        }

        private void TimerFinished()
        {
            _timing = false;

            if (_trapUISet)
            {
                SetTrapUI(false);
            }
            else
            {
                Round.Instance.EndRound();
            }

        }

        private void FixedUpdate()
        {
            if (Base.TickBase.IsPaused())
            {
                if (!_pauseState)
                {
                    _pauseState = true;

                    _resumeTime = _currentTimer - Time.realtimeSinceStartup;
                }

                return;
            }
            else if (_pauseState)
            {
                _currentTimer = Time.realtimeSinceStartup + _resumeTime;
                _pauseState = false;
            }


            if (!_timing)
            {
                _timer.text = "";

                return;
            }

            if (_timer != null && _timing && _currentTimer > Time.realtimeSinceStartup)
                _timer.text = _timerText.Replace("{x}", "" + (int)(_currentTimer - Time.realtimeSinceStartup));
            else if (_timing && _currentTimer <= Time.realtimeSinceStartup)
                TimerFinished();
        }

        private void OnDestroy()
        {
            DestructionHeap.PrepareForDestruction(Instance);
            Instance = null;
        }
    }
}
