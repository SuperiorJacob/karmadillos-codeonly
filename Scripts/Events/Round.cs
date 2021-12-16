using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug),
        EditorTools.AberrationDescription("Round loading and running event system.", "Jacob Cooper", "23/09/2021")]
    public class Round : MonoBehaviour
    {
        public static Round Instance;

        [EditorTools.AberrationToolBar("References")]
        [SerializeField, EditorTools.AberrationRequired()] private AssetReference _podiumPrefab;
        [SerializeField, EditorTools.AberrationRequired()] private AssetReference _gameUIScene;
        [SerializeField, EditorTools.AberrationRequired()] private AssetReference _endScreenScene;
        [SerializeField] private Animator _resetAnimator;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Settings")]
        [SerializeField, EditorTools.AberrationRequired, Range(0.1f, 10f)] private float _countDownAmount;
        [SerializeField, EditorTools.AberrationRequired, Range(0.1f, 20f)] private float _podiumLoadSpeed;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Build")]
        [SerializeField] private Material _buildableMaterial;
        [SerializeField] private Material _nonBuildableMaterial;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Collider[] _buildEnabled;
        [SerializeField] private GameObject[] _buildDisable;
        [SerializeField] private MeshRenderer[] _buildable;
        [SerializeField] private MeshRenderer[] _nonBuildable;
        [SerializeField] private MeshRenderer[] _outlineable;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Events")]
        public float thresholdEventPush = 1000f;
        public UnityEvent postStartRound;
        public UnityEvent postFinishCountDown;
        public UnityEvent postEndRound;

        [HideInInspector] public Base.PlayerLoader prevLoader = null;


        private Vector3 _thresholdEventDir;
        private Rigidbody _thresholdEventRb;
        private bool _countingDown = false;
        private bool _inRound = false;
        private bool _uiLoaded = false;
        private float _timer = 0;
        private int _round = 0;
        private bool _finishedCountingDown = false;
        private float _aboutToShoot = 0f;

        private Dictionary<MeshRenderer, Material> _matsOnLoad;

        private List<(GameObject podium, Vector3 goal)> _podiums;

        [EditorTools.AberrationDescription("Checks if we are currently in a match.", "Jacob Cooper", "3/11/2021")]
        public static bool IsIngame()
        {
            return Instance != null && Instance.IsInRound() && Instance.FinishedCountDown() && !Base.Players.BlueprintMode;
        }

        [EditorTools.AberrationDescription("Get round number.", "Jacob Cooper", "12/11/2021")]
        public int GetRound()
        {
            return _round;
        }

        [EditorTools.AberrationDescription("Checks if the game has finished counting down.", "Jacob Cooper", "3/11/2021")]
        public bool FinishedCountDown()
        {
            return _finishedCountingDown;
        }


        [EditorTools.AberrationDescription("Checks if the game is currently in a round.", "Jacob Cooper", "3/11/2021")]
        public bool IsInRound()
        {
            return _inRound;
        }

        [EditorTools.AberrationDescription("Plays cool knock event.", "Jacob Cooper", "3/11/2021")]
        public void ThresholdReachedEvent(Base.PlayerBase a_victim, Base.PlayerBase a_attacker, Vector3 a_direction)
        {
            Rigidbody rb = a_victim.GetRigidBody();

            _thresholdEventRb = rb;
            _thresholdEventDir = (a_direction + Vector3.up);

            Vector3 camPos = (a_victim.transform.position + a_attacker.transform.position) / 2;
            Base.PlayerLoader.Instance.SetForcedCamera(true, camPos, 3f);

            _aboutToShoot = Time.realtimeSinceStartup + 0.5f;

            rb.velocity = Vector3.zero;

            if (a_victim.knockParticles.particleHolder != null)
            {
                a_victim.knockParticles.particleHolder.position = camPos;

                a_victim.knockParticles.Play(true);
            }

            Base.TickBase.LocalTimeScale = 0f;

            rb.detectCollisions = false;

            //a_victim.sendIt = true;

            a_attacker.GetRigidBody().velocity = Vector3.zero;
            a_victim.SetControllerVibration(0.9f, 0.9f, 1);

            StartCoroutine(PlayerDeath(a_victim.GetPlayerIndex(), a_attacker.GetPlayerIndex(), 1.5f, 1f, a_victim));
        }

        [EditorTools.AberrationDescription("Disables player.", "Jacob Cooper", "14/10/2021")]
        public void DisablePlayer(GameObject a_player, bool a_disabled)
        {
            if (a_player != null && a_player.TryGetComponent(out Base.PlayerBase pb))
            {
                pb.playerState = a_disabled ? Base.PlayerBase.ControlState.Disabled : Base.PlayerBase.ControlState.Enabled;

                Rigidbody rb = pb.GetRigidBody();
                if (rb != null)
                    rb.isKinematic = a_disabled;

                //Debug.Log(pb.playerState + " " + a_disabled);
            }
        }

        [EditorTools.AberrationDescription("Basically freezes all players.", "Jacob Cooper", "23/09/2021")]
        public void DisablePlayerPhysics(bool a_disabled)
        {
            foreach (var player in Base.Players.GetActivePlayers())
            {
                DisablePlayer(player.player, a_disabled);
            }
        }

        [EditorTools.AberrationDescription("Pre round initialization.", "Jacob Cooper", "23/09/2021")]
        public void PreStartRound()
        {
            _countingDown = true;
            _timer = Time.realtimeSinceStartup + _countDownAmount;

            //Time.timeScale = 0f;

            DisablePlayerPhysics(true);

            Utils.RichPresenceHandler.UpdateActivity(Utils.RichPresenceHandler.RpcSettings.inGameState.Replace("{round}", "" + _round), 
                MapLoader.nextScene, Utils.RichPresenceHandler.GetMap(MapLoader.nextScene), MapLoader.nextScene, 
                "ui_player1", "Player 1", 4, Base.Players.PlayerDictionary.Count, "14814u8fa");

            if (!_uiLoaded)
                Addressables.LoadSceneAsync(_gameUIScene, UnityEngine.SceneManagement.LoadSceneMode.Additive).Completed += handle =>
                {
                    _uiLoaded = true;

                    _timer = Time.realtimeSinceStartup + _countDownAmount;
                    GameUILoad.Instance.animatorCountdown.Play("Play");
                };
            else
                GameUILoad.Instance.animatorCountdown.Play("Play");
        }

        [EditorTools.AberrationDescription("Round initilization.", "Jacob Cooper", "23/09/2021")]
        public IEnumerator StartRound()
        {
            _round++;

            if (_round > 1)
            {
                GameUILoad.Instance.Setup();

                if (prevLoader != null)
                    Base.PlayerLoader.Instance = prevLoader;
            }

            Base.PlayerLoader.Instance.BluePrintMode(false);

            UpdateBuildMaterials();

            _podiums = new List<(GameObject podium, Vector3 goal)>();

            Base.PlayerLoader.Instance.ResetInfo();
            Base.PlayerLoader.Instance.LoadPlayers();

            _inRound = true;
            _finishedCountingDown = false;

            ResetSceneAnimations(false);

            yield return new WaitForSeconds(0.5f);

            PreStartRound();

            postStartRound.Invoke();
        }

        [EditorTools.AberrationDescription("Get the winner from the list.", "Jacob Cooper", "23/09/2021")]
        public int GetWinner()
        {
            int score = 0;
            int playerID = 0;

            foreach (var player in Base.Players.GetActivePlayers())
            {
                if (player.score > score)
                {
                    score = player.score;
                    playerID = player.playerID;
                }
            }

            return playerID;
        }

        [EditorTools.AberrationDescription("Trap Placement and next round loading.", "Jacob Cooper", "23/09/2021")]
        public IEnumerator BeginNextRound()
        {
            GameUILoad.Instance.EnableBreakdown(true);
            _inRound = false;
            Base.TickBase.LocalTimeScale = 0;

            yield return new WaitForSeconds(3f);

            Base.TickBase.LocalTimeScale = 1;
            GameUILoad.Instance.EnableBreakdown(false);

            prevLoader = Base.PlayerLoader.Instance;
            Base.PlayerLoader.Instance.BluePrintMode(true);
            UpdateBuildMaterials();
            ResetSceneAnimations(true);
            GameUILoad.Instance.SetTrapUI(true);

        }

        [EditorTools.AberrationDescription("Pre finish counting down.", "Jacob Cooper", "23/09/2021")]
        public void PreFinishedCountDown()
        {
            _countingDown = false;

            _timer = 0;

            foreach (var player in Base.Players.GetActivePlayers())
                player.player?.transform.SetParent(null);

            for (int i = 0; i < _podiums.Count; i++)
            {
                var pod = _podiums[i];

                DestructionHeap.PrepareForDestruction(pod.podium);

                pod.podium = null;

                _podiums[i] = pod;
            }

            //Time.timeScale = 1.0f;
            DisablePlayerPhysics(false);

            GameUILoad.Instance.SetTimerTime(Time.realtimeSinceStartup + Settings.Instance.settingsReference.gamePhaseTimer);
        }

        [EditorTools.AberrationDescription("Finish counting down and start the round.", "Jacob Cooper", "23/09/2021")]
        public void FinishCountingDown()
        {
            PreFinishedCountDown();

            postFinishCountDown.Invoke();

            _finishedCountingDown = true;
        }

        [EditorTools.AberrationDescription("End of the round (final one standing).", "Jacob Cooper", "23/09/2021")]
        public void EndRound(bool a_force = false)
        {
            _countingDown = false;

            postEndRound.Invoke();

            _inRound = false;

            if (a_force || _round >= (Settings.Instance != null ? Settings.Instance.settingsReference.maxRound : 3))
            {
                Addressables.LoadSceneAsync(_endScreenScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
                StartCoroutine(BeginNextRound());
        }

        [EditorTools.AberrationDescription("Returns how many players are alive.", "Jacob Cooper", "23/09/2021")]
        public int PlayersAlive()
        {
            int count = Base.Players.PlayerDictionary.Count;
            foreach (var player in Base.Players.GetActivePlayers())
            {
                if (player.dead)
                    count--;
            }

            return count;
        }

        [EditorTools.AberrationDescription("When a player dies event.", "Jacob Cooper", "23/09/2021")]
        public void PlayerDeath(int a_victimID, int a_attackerID)
        {
            if (!_inRound)
                return;

            Base.PlayerData vicData = Base.Players.PlayerDictionary[a_victimID];

            vicData.dead = true;

            if (vicData.player.TryGetComponent(out Collider collider))
                collider.enabled = false;

            Base.Players.PlayerDictionary[a_victimID] = vicData;

            GameUILoad.Instance.SetDead(a_victimID, true);

            bool endGame = PlayersAlive() <= 1;

            if (endGame)
            {
                int id = 0;

                foreach (var player in Base.Players.PlayerDictionary)
                {
                    if (player.Value.dead || a_victimID == player.Value.playerID)
                        continue;

                    id = player.Value.id;
                }

                Base.PlayerData attData = Base.Players.PlayerDictionary[id];
                attData.score += 1;

                Base.Players.PlayerDictionary[id] = attData;
                GameUILoad.Instance.SetScore(id, attData.score);

                EndRound(attData.score >= Settings.Instance.settingsReference.maxScoreToWin);
            }
        }

        [EditorTools.AberrationDescription("Timed player death.", "Jacob Cooper", "23/09/2021")]
        public IEnumerator PlayerDeath(int a_victimID, int a_attackerID, float a_delay = 0f, float a_secondDelay = 0f, Base.PlayerBase a_victimbase = null)
        {
            yield return new WaitForSeconds(a_delay);

            a_victimbase?.HitScreen();

            // Stop camera render :)
            Base.PlayerData vicData = Base.Players.PlayerDictionary[a_victimID];
            vicData.dead = true;

            Base.Players.PlayerDictionary[a_victimID] = vicData;

            yield return new WaitForSeconds(a_secondDelay);

            Base.PlayerLoader.Instance.SetForcedCamera(false);

            PlayerDeath(a_victimID, a_attackerID);
        }

        [EditorTools.AberrationDescription("Load the players podium and move them down.", "Jacob Cooper", "23/09/2021")]
        public void LoadPlayerPodium(Transform a_player, Vector3 a_position, Vector3 a_goal)
        {
            Vector3 dir = (Vector3.up) / 2;

            Addressables.InstantiateAsync(_podiumPrefab, a_position - dir, Quaternion.identity, null, true).Completed += handle =>
            {
                a_player.transform.position = a_position + dir;
                a_player.transform.SetParent(handle.Result.transform);

                _podiums.Add((handle.Result, a_goal));
            };
        }


        [EditorTools.AberrationDescription("Reset the scene animator.", "Jacob Cooper", "3/11/2021")]
        public void ResetSceneAnimations(bool a_buildMode)
        {
            if (_resetAnimator == null)
                return;

            _resetAnimator.Rebind();
            _resetAnimator.Update(0f);

            _resetAnimator.SetBool("Build", a_buildMode);
        }


        [EditorTools.AberrationDescription("Starts the game.", "Jacob Cooper", "3/11/2021")]
        public void StartGame()
        {
            StartCoroutine(StartRound());
        }


        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {

            // TIME SCALER
            if (_thresholdEventRb != null && _aboutToShoot < Time.realtimeSinceStartup && Base.TickBase.LocalTimeScale != Base.TickBase.BaseScale)
            {
                Base.TickBase.LocalTimeScale = Base.TickBase.BaseScale;
                Base.PlayerLoader.Instance.SetForcedCamera(false);

                _thresholdEventRb.AddForce(_thresholdEventDir * thresholdEventPush, ForceMode.Impulse);

                _thresholdEventRb = null;
            }

            if (_countingDown && _timer < Time.realtimeSinceStartup
                && !_finishedCountingDown)
                //&& _countDownEvent.isPlaying)
            {
                FinishCountingDown();
            }
            else if (_countingDown)
            {
                foreach (var podium in _podiums)
                {
                    if (DestructionHeap.ObjectNotOnHeap(podium.podium))
                        podium.podium.transform.position = Vector3.Lerp(podium.podium.transform.position, podium.goal, Time.deltaTime * _podiumLoadSpeed);
                }
            }
        }

        [EditorTools.AberrationDescription("Update build materials.", "Jacob Cooper", "4/11/2021")]
        private void UpdateBuildMaterials()
        {
            foreach (var build in _buildEnabled)
                build.enabled = Base.Players.BlueprintMode;

            foreach (var build in _buildDisable)
                build.SetActive(!Base.Players.BlueprintMode);

            foreach (var mesh in _buildable)
                mesh.material = Base.Players.BlueprintMode ? _buildableMaterial : _matsOnLoad[mesh];

            foreach (var mesh in _nonBuildable)
                mesh.material = Base.Players.BlueprintMode ? _nonBuildableMaterial : _matsOnLoad[mesh];

            foreach (var mesh in _outlineable)
                mesh.material = Base.Players.BlueprintMode ? _outlineMaterial : _matsOnLoad[mesh];
        }

        private void Start()
        {
            _matsOnLoad = new Dictionary<MeshRenderer, Material>();

            // Load materials and meshes.
            foreach (var mesh in _buildable)
                _matsOnLoad[mesh] = mesh.material;

            foreach (var mesh in _nonBuildable)
                _matsOnLoad[mesh] = mesh.material;

            foreach (var mesh in _outlineable)
                _matsOnLoad[mesh] = mesh.material;
        }
    }
}
