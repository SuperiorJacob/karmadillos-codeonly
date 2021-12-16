using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;

namespace AberrationGames.Base
{
    // Really nice way to save data across scenes without having to use
    // DontDestroyOnLoad ( GameObject )

    public static class Players
    {
        public static Dictionary<int, PlayerData> PlayerDictionary;
        public static bool ChangingScenes = false;
        public static bool BlueprintMode = false;

        public static void CleansePlayerGameObjects()
        {
            for (int i = 0; i < PlayerDictionary.Count; i++)
            {
                var p = PlayerDictionary[i];
                p.player = null;

                PlayerDictionary[i] = p;
            }
        }

        public static List<PlayerData> GetActivePlayers()
        {
            List<PlayerData> data = new List<PlayerData>();

            foreach (var p in PlayerDictionary)
            {
                var player = p.Value;

                if (player.player != null && DestructionHeap.ObjectOnHeap(player.player))
                    player.player = null;

                data.Add(player);
            }

            return data;
        }
    }

    public struct PlayerData
    {
        public GameObject player;
        public PlayerInput input;
        public InputDevice device;
        public Color color;
        public int id;
        public int playerID;
        public int score;
        public bool dead;
        public bool disconnected;
    }

    public struct ObstructionData
    {
        public int id;
        public float timeStamp;
        public Material oldMaterial;
        public MeshRenderer mesh;
    }

    /// <summary>
    /// Designed for loading different types of players into the scene.
    /// </summary>
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug), 
        EditorTools.AberrationDescription("Designed for loading different types of players into the scene.", "Jacob Cooper", "18/08/2021")]
    public class PlayerLoader : MonoBehaviour
    {
        public static PlayerLoader Instance;

        [EditorTools.AberrationToolBar("References")]
        [EditorTools.AberrationRequired] public AssetReference playerPrefab;
        [SerializeField] private UnityEngine.Rendering.Volume _processingVolume;
        //[EditorTools.AberrationRequired] public AssetReference obstructionMaterial;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("World")]
        [EditorTools.AberrationRequired] public Transform[] spawnPositions;
        public Transform canvas;
        public CinemachineVirtualCamera vcam;
        [HideInInspector] public CinemachineBasicMultiChannelPerlin noiseCam;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Settings")]

        [SerializeField] private bool _resetOnLoad = false;
        [SerializeField] private float _distMult = 2f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _moveSpeed = 10f;

        [SerializeField] private Vector3 _buildModeCamera = new Vector3(0, 20, -0.01f);
        [SerializeField] private Vector3 _playModeCamera = new Vector3(0, 18, -25);
        [SerializeField] private float _orthoGraphicMult = 1f;

        [SerializeField, EditorTools.AberrationRequired, Range(0.1f, 50f)] private float _spawnMult = 1f;

        [EditorTools.AberrationEndToolBar]
        [EditorTools.AberrationToolBar("Events")]
        [Header("TODO: Currently only supports the UI.")]
        public UnityEvent<PlayerInput, int> onClickPress;
        public UnityEvent<PlayerInput, int> onReloadPress;
        public UnityEvent<PlayerInput, int> onBackPress;

        private bool _forceCameraPosition = false;
        private Vector3 _forcedPosition;
        private float _forcedCameraZoom;

        private CinemachineTransposer _poser;
        private bool _createdPlayers = false;

        //private Material _cachedObstructionMat;
        private Dictionary<int, ObstructionData> _cachedObstruction;

        public void SetForcedCamera(bool a_shouldForce, Vector3 a_position = default, float a_zoom = 0f)
        {
            _forceCameraPosition = a_shouldForce;
            _forcedPosition = a_position;
            _forcedCameraZoom = a_zoom;
        }

        /// <summary>
        /// Asynchonously loads the player into the scene.
        /// </summary>
        /// <param name="a_handle">Async handle for loading the player in.</param>
        /// <param name="a_data">The referenced data to reload the player with.</param>
        /// <returns>Loaded player data.</returns>
        [EditorTools.AberrationDescription("Asynchonously loads the player into the scene.", "Jacob Cooper", "17/08/2021")]
        public PlayerData LoadPlayer(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> a_handle, PlayerData a_data)
        {
            GameObject ply = a_handle.Result;
            if (ply == null)
                return new PlayerData { };

            var playerReference = Settings.Instance.settingsReference.playerDataReference.players[a_data.id];

            Color col = a_data.color;

            // Checks if its the main menu.
            if (ply.CompareTag("Cursor") && canvas != null)
            {
                ply.GetComponent<Image>().color = col;

                if (ply.TryGetComponent(out MainMenuPlayer cursor))
                {
                    cursor.SetPlayerIndex(a_data.id);
                    cursor.inputDevice = a_data.device;
                }
            }
            else
            {
                if (ply.TryGetComponent(out PlayerBase player))
                {
                    player.SetPlayerIndex(a_data.id);
                    player.inputDevice = a_data.device;

                    foreach (var shellPart in player.characterShell)
                    {
                        shellPart.material.color = new Color(col.r, col.g, col.b, 0.3f);
                    }

                    player.trail.material?.SetColor("Colour", new Color(col.r, col.g, col.b, 0.8f));
                }

                Events.Round.Instance.DisablePlayer(ply, true);

                if (player.fade != null)
                    player.fade.Begin(playerReference.playerIcon, col);
            }

            // Reloading old player data with the new gameobject.
            return new PlayerData {
                id = a_data.id,
                player = ply,
                input = ply.GetComponent<PlayerInput>(),
                device = a_data.device,
                playerID = a_data.playerID,
                score = a_data.score,
                dead = false,
                color = a_data.color,
                disconnected = a_data.disconnected
            };
        }

        /// <summary>
        /// Gets the central position between all of the current players.
        /// </summary>
        /// <returns>A position between all players.</returns>
        [EditorTools.AberrationDescription("Gets the central position between all of the current players.", "Jacob Cooper", "18/08/2021")]
        public (Vector3 pos, float dist) GetCentralPosition()
        {
            if (_forceCameraPosition)
            {
                return (_forcedPosition, _forcedCameraZoom);
            }

            if (Players.BlueprintMode || Networking.Shared.NetworkInformation.Realm == Networking.Shared.NetworkRealm.Server)
            {
                return (new Vector3(0, 0, 0), 1 * _distMult);
            }

            Vector3 sum = Vector3.zero;

            Dictionary<int, PlayerData> cachedPlayers = new Dictionary<int, PlayerData>();

            foreach (var ply in Players.PlayerDictionary)
            {
                if (ply.Value.dead)
                    continue;

                var val = ply.Value;

                if (DestructionHeap.ObjectOnHeap(ply.Value.player))
                    val.player = null;

                cachedPlayers[ply.Key] = val;
            }

            // Cursed but good for debugging.
            if (cachedPlayers.Count < 1)
            {
#if UNITY_EDITOR
                GameObject obj = GameObject.FindGameObjectWithTag("User");
                if (obj != null)
                    cachedPlayers = new Dictionary<int, PlayerData>()
                    {
                        [0] = new PlayerData { player = obj }
                    };
#endif
                if (cachedPlayers.Count < 1)
                    return (sum, 0);
            }

            float furthest = 0f;
            Vector3 furthestPlayer = Vector3.zero;

            foreach (var player in cachedPlayers)
            {
                Vector3 pos = player.Value.player != null ? player.Value.player.transform.position : Vector3.zero;

                if (cachedPlayers.Count == 1)
                    return (pos, 0);

                sum += pos;

                float dist = Vector3.Distance(pos, furthestPlayer);
                if (dist > furthest)
                {
                    furthest = dist;
                    furthestPlayer = pos;
                }
            }

            return (sum / cachedPlayers.Count, furthest * _distMult);
        }

        /// <summary>
        /// Reset player loader data changed.
        /// </summary>
        [EditorTools.AberrationDescription("Reset player loader data changed.", "Jacob Cooper", "08/10/2021")]
        public void ResetInfo()
        {
            _createdPlayers = false;
        }

        /// <summary>
        /// Starts the coroutine that loads the players in.
        /// </summary>
        [EditorTools.AberrationDescription("Starts the coroutine that loads the players in.", "Jacob Cooper", "08/10/2021")]
        public void LoadPlayers(float a_time = 0f)
        {
            if (Players.PlayerDictionary.Count > 0 && !_createdPlayers)
            {
                _createdPlayers = true;

                foreach (var obj in GameObject.FindGameObjectsWithTag("User"))
                    DestructionHeap.PrepareForDestruction(obj);

                StartCoroutine(LoadingPlayers(a_time));
            }
        }

        public void TrapLoading()
        {
            _spawnMult = 0;

            if (spawnPositions == null || spawnPositions.Length < 1)
                spawnPositions = new Transform[] { transform, transform, transform, transform };

            LoadPlayers();
        }

        /// <summary>
        /// Initializes important data and attempts to load the players in.
        /// </summary>
        [EditorTools.AberrationDescription("Initializes important data and attempts to load the players in.", "Jacob Cooper", "18/08/2021")]
        public void Start()
        {
            if (Players.BlueprintMode)
                return;

            if (Instance != null && vcam == null)
                vcam = Instance.vcam;

            if (vcam != null)
                noiseCam = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            // Allow the playermanager to update now.
            Players.ChangingScenes = false;

            if (spawnPositions == null) spawnPositions = new Transform[] { transform, transform, transform, transform };

            Debug.Log("Loading players | Should be networked: " + (Networking.Shared.NetworkInformation.IsNetworking && !Networking.Shared.NetworkInformation.ShouldSpawnLocal));

            if (Networking.Shared.NetworkInformation.IsNetworking && !Networking.Shared.NetworkInformation.ShouldSpawnLocal)
                return;

            // Re initialize old data.
            if (Players.PlayerDictionary == null || _resetOnLoad)
            {
                Players.PlayerDictionary = new Dictionary<int, PlayerData>();
            }

            // If we came from the main menu or another scene, it will
            // have to reload the players again.
        }

        /// <summary>
        /// Used by the Input Manager to queuee players in.
        /// </summary>
        [EditorTools.AberrationDescription("Used by the Input Manager to queue players in.", "Jacob Cooper", "18/08/2021")]
        public void PlayerJoin(PlayerInput a_input)
        {
            var playerReference = Settings.Instance.settingsReference.playerDataReference.players[Players.PlayerDictionary.Count];

            // REFACTOR CODE IN HERE TO WORK LAZYNESS MAKE A METHOD FOR SETTING DATAA!
            // Checks if its the main menu.
            if (a_input.gameObject.CompareTag("Cursor") && canvas != null)
            {
                a_input.transform.SetParent(canvas, false);
                a_input.GetComponent<Image>().color = playerReference.color;

                if (a_input.gameObject.TryGetComponent(out MainMenuPlayer cursor))
                {
                    cursor.SetPlayerIndex(Players.PlayerDictionary.Count);
                }
            }
            else
            {
                a_input.gameObject.transform.position = spawnPositions.Length > 0 ? spawnPositions[0].position : transform.position;
            }

            // Create the player information that is stored globally.
            int id = a_input.user.index;

            // Probs fix this stuff up more ..
            Players.PlayerDictionary[id] = new PlayerData
            {
                id = id,
                player = a_input.gameObject,
                input = a_input,
                device = a_input.devices[0],
                playerID = Players.PlayerDictionary.Count,
                score = 0,
                color = playerReference.color
            };

            if (canvas != null)
                foreach (var card in canvas.GetComponentsInChildren<Events.PlayerCard>())
                {
                    card.isMaster = id == 0;
                    
                    if (id == card.playerID)
                        card.FindPlayer(id);
                }
        }

        /// <summary>
        /// Disables all player objects in the scene.
        /// </summary>
        [EditorTools.AberrationDescription("Disables all player objects in the scene.", "Jacob Cooper", "18/08/2021")]
        public void DisablePlayers()
        {
            foreach (var obj in GameObject.FindGameObjectsWithTag("User"))
            {
                DestructionHeap.PrepareForDestruction(obj);
            }

            Players.CleansePlayerGameObjects();
        }

        /// <summary>
        /// Toggles the blue print mode (allows players to spawn traps).
        /// </summary>
        [EditorTools.AberrationDescription("Toggles the blue print mode (allows players to spawn traps).", "Jacob Cooper", "18/08/2021")]
        public void BluePrintMode(bool a_mode)
        {
            Players.BlueprintMode = a_mode;

            _createdPlayers = false;

            if (_processingVolume != null)
                _processingVolume.enabled = !a_mode;

            if (Camera.main != null)
                if (a_mode)
                {
                    Camera.main.clearFlags = CameraClearFlags.SolidColor;
                    Camera.main.orthographic = true;
                    vcam.m_Lens.OrthographicSize = Settings.Instance.settingsReference.buildModeOrthographicSize * _orthoGraphicMult;

                    if (noiseCam != null)
                        noiseCam.m_AmplitudeGain = 0f;

                    _poser = vcam.GetCinemachineComponent<CinemachineTransposer>();
                    _poser.m_FollowOffset = _buildModeCamera;
                    _poser.m_XDamping = 0;
                    _poser.m_YDamping = 0;
                    _poser.m_ZDamping = 0;

                    DisablePlayers();
                }
                else
                {
                    Camera.main.clearFlags = CameraClearFlags.Skybox;
                    Camera.main.orthographic = false;

                    _poser = vcam.GetCinemachineComponent<CinemachineTransposer>();
                    _poser.m_FollowOffset = _playModeCamera;
                    _poser.m_XDamping = 1;
                    _poser.m_YDamping = 1;
                    _poser.m_ZDamping = 1;
                }
        }

        /// <summary>
        /// Resets the scene.
        /// </summary>
        [EditorTools.AberrationDescription("Resets the scene.", "Jacob Cooper", "18/08/2021")]
        public void SceneReset(AsyncOperation a_op)
        {
            Start();
        }

        [EditorTools.AberrationDescription("Player loading queue coroutine.", "Jacob Cooper", "18/08/2021")]
        public IEnumerator LoadingPlayers(float a_time = 0f, bool a_podium = true)
        {
            if (a_time > 0)
                yield return new WaitForSeconds(a_time);

            // Randomizes spawn positions.
            System.Random rnd = new System.Random();
            spawnPositions = spawnPositions.OrderBy(x => rnd.Next()).ToArray();

            // Loads players in
            var players = Base.Players.GetActivePlayers();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerData player = players[i];
                if (player.disconnected)
                    continue;

                bool isDone = false;
                // Player is pushed up so it can "come down" into position.
                Addressables.InstantiateAsync(playerPrefab, spawnPositions[i].position + (Vector3.up * _spawnMult), spawnPositions[i].rotation, canvas != null ? canvas : null, true).Completed += handle =>
                {
                    isDone = true;
                    Players.PlayerDictionary[i] = LoadPlayer(handle, player);

                    if (!Players.BlueprintMode && a_podium)
                        Events.Round.Instance?.LoadPlayerPodium(handle.Result.transform, handle.Result.transform.position, spawnPositions[i].position);
                };

                while (!isDone)
                {
                    yield return null;
                }

                yield return null;
            }
        }

        [EditorTools.AberrationDescription("Initializes important data.", "Jacob Cooper", "18/08/2021")]
        private void Awake()
        {
            Instance = this;

            _cachedObstruction = new Dictionary<int, ObstructionData>();

            if (Camera.main.gameObject.TryGetComponent(out CinemachineBrain brain))
                brain.enabled = true;

            //if (obstructionMaterial != null)
            //    Addressables.LoadAssetAsync<Material>(obstructionMaterial).Completed += handle =>
            //    {
            //        if (handle.Status == AsyncOperationStatus.Succeeded)
            //            _cachedObstructionMat = handle.Result;
            //    };
        }

        private void OnEnable()
        {
            if (Players.BlueprintMode)
            {
                _createdPlayers = false;
            }
        }

        [EditorTools.AberrationDescription("Fades objects that get in the way of the players.", "Jacob Cooper", "18/08/2021")]
        private void FixVisualObstacles(Dictionary<int, PlayerData> a_players = null)
        {
            if (a_players == null)
                a_players = Players.PlayerDictionary;

            Transform cam = Camera.main.transform;

            foreach (var player in a_players)
            {
                PlayerData ply = player.Value;
                if (ply.player == null)
                    continue;
                RaycastHit[] hits = Physics.RaycastAll(transform.position, (cam.position - transform.position).normalized, Mathf.Infinity, 1 << 9);

                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.TryGetComponent(out MeshRenderer render))
                    {
                        if (_cachedObstruction.TryGetValue(hit.transform.GetInstanceID(), out ObstructionData obstruct))
                            _cachedObstruction[hit.transform.GetInstanceID()] = new ObstructionData { id = obstruct.id, mesh = render, oldMaterial = obstruct.oldMaterial, timeStamp = Time.realtimeSinceStartup + 0.25f };
                        else
                             _cachedObstruction[hit.transform.GetInstanceID()] = new ObstructionData { id = ply.id, mesh = render, oldMaterial = render.material, timeStamp = Time.realtimeSinceStartup + 0.25f };

                        //render.material = _cachedObstructionMat;
                    }
                }
            }

            foreach (var obstructionData in _cachedObstruction)
            {
                ObstructionData data = obstructionData.Value;

                if (data.timeStamp < Time.realtimeSinceStartup)
                {
                    data.mesh.material = data.oldMaterial;
                }
            }
        }

        [EditorTools.AberrationDescription("Updates camera information.", "Jacob Cooper", "18/08/2021")]
        private void Update()
        {
            if (vcam == null) return;

            if (noiseCam != null)
                noiseCam.m_AmplitudeGain = Mathf.Clamp(noiseCam.m_AmplitudeGain - Time.deltaTime, 0, 100f);

            (Vector3 pos, float dist) central = GetCentralPosition();

            float cameraZoomOutBase = Settings.Instance.settingsReference.cameraZoomOutBase;
            Vector2 cameraZoomClamp = Settings.Instance.settingsReference.cameraZoomOutClamp;
            float d = _forceCameraPosition ? central.dist : (Players.BlueprintMode ? cameraZoomOutBase : cameraZoomOutBase * Mathf.Clamp(central.dist, cameraZoomClamp.x, cameraZoomClamp.y) / cameraZoomClamp.y);

            Vector3 newPos = central.pos;
            vcam.m_Lens.FieldOfView = Mathf.Lerp(vcam.m_Lens.FieldOfView, d, Time.deltaTime * _zoomSpeed);

            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * _moveSpeed);
        }

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}