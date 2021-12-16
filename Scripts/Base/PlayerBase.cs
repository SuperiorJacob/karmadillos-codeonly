
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AberrationGames.Base
{
    /// <summary>
    /// Base player class used for controlling each player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(PlayerInput))]
    [EditorTools.AberrationDescription("Designed to provide realtime physics iteractions to the player", "Duncan Sykes", "2/11/2021")]
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug)]
    public class PlayerBase : MechanicLoader
    {
        [System.Serializable]
        public enum State
        {
            Ready,
            Cooldown
        }

        [System.Serializable]
        public enum ControlState
        {
            Enabled,
            Disabled,
            Frozen,
            Shocked,
            Stunned
        }

        [System.Serializable]
        public struct ParticleReference
        {
            public Transform particleHolder; // Master object that controls particle children.
            public float up;
            public ParticleSystemRenderer[] matOverrides;
            public ParticleSystem[] particleSystems;

            public void Detach()
            {
                if (particleHolder != null)
                    particleHolder.parent = null;

                Play(false);
            }

            public void SetMatOverrideColour(Color a_color, bool a_lerp = false, float a_delta = 0)
            {
                foreach (var part in matOverrides)
                {
                    if (a_lerp)
                        part.material.color = Color.Lerp(part.material.color, a_color, a_delta);
                    else
                        part.material.color = a_color;
                }
            }

            /// <summary>
            /// Play or Stop all particles referenced.
            /// </summary>
            /// <param name="a_shouldPlay"></param>
            public void Play(bool a_shouldPlay, bool a_shouldActive = false)
            {
                if (a_shouldActive)
                    particleHolder.gameObject.SetActive(a_shouldPlay);

                foreach (var particle in particleSystems)
                {
                    if (particle == null)
                        continue;

                    if (a_shouldPlay)
                        particle.Play();
                    else
                        particle.Stop();
                }
            }
        }

        // Because we hate unity.
        public new Transform transform { get => _transform; }

        #region Player Controller Settings

        [EditorTools.AberrationToolBar("Player Controller")]
        [Header("Movement")]
        [Tooltip("Acceleration Rate of the player")]
        public float acceleration;

        [Range(0.500f, 0.999f)]
        [Tooltip("Smooths out velocity when player reaches max speed. 0.5 = choppier, 0.9999... smoother")] 
        public float movementSmoothing = 0.967f;

        [Tooltip("Maximum speed of the player")]
        public float maxSpeed;

        [Tooltip("Maximum charge magnitude")]
        public float maxVelocityMagnitude;

        [Tooltip("Maximum magnitude to cancel knockback.")]
        public float cancelKnockbackLength = 0.5f;

        [Tooltip("If the player has access to the controls")]
        public bool skidding = false;

        [Header("Rumble")]
        public float bumpAmpLow = 0.25f;
        public float bumpAmpHigh = 0.25f;
        public float bumpDuration = 0.1f;
        public float rumbleSmoothing = 10f;
        public float maxRumbleCharge = 0.7f;

        [Header("Charge Control")]
        public float cooldownTimer;

        [Tooltip("How long before the player can use the charge abilty (in seconds)")]
        public float chargeCooldown;

        [Tooltip("How strong the knockback effect of a player collision is")]
        public float knockbackAmount;

        [Tooltip("How quicky the player brakes")]
        public float brakeFactor;

        [Tooltip("The Maximum force of the charge abilty")]
        public float chargeForce;

        [Tooltip("Maximum charge particle scale")]
        public float maxChargeParticleScale = 2f;

        [Tooltip("Max charge rot speed")]
        public float chargeRotMaxSpeed = 200;

        [Tooltip("How long it takes to charge up the charge abilty (in seconds")]
        public float maxChargeTime;

        [Tooltip("How long it takes to charge up and be stunned")]
        public float maxChargeBeforeStun;

        public float maxTrailSize = 1f;

        [Header("Stun")]
        [Tooltip("Amount of time player is stunned for after successful charge (out of energy)")]
        public float stunnedTimer = 0.5f;

        [Tooltip("Over charge amount to stun")]
        public float stunOnDurationPercentile = 1.5f;

        [Tooltip("If hit whilst charging how long do we stun? Percentage")] public float chargeCancelStunPercent = 2f;

        [Header("Threshold")]
        [Tooltip("Maximum knock onto the player threshold to trigger hit event."),
            Range(1f, 1000f)]
        public float maximumThreshold = 1f;

        [Tooltip("Knock increase onto threshold."), Range(0f, 1000f)]
        public float thresholdIncrease;

        [Tooltip("Threshold decrease over time."), Range(0f, 1000f)]
        public float thresholdDecrease;

        [Tooltip("The multiplier amount it knocks back.")]
        public float thresholdKnockbackMultiplier = 5f;

        [Tooltip("The multiplier amount it adds to threshold.")]
        public float thresholdMagnitudeMultiplier = 5f;

        [HideInInspector] public ControlState playerState;
        [HideInInspector] public bool sendIt = false;
        [HideInInspector] public State chargeState;
        [HideInInspector] public Vector2 StickDir
        {
            get => _stickDir;
            private set => _stickDir = value;
        }
        [EditorTools.AberrationEndToolBar]
        #endregion

        #region Particle Fields
        [EditorTools.AberrationToolBar("Effects")]
        public Color normalChargeColour;
        public Color burnOutColour;

        public ParticleReference splashParticles;
        public ParticleReference chargeParticles;
        public ParticleReference stunParticles;
        public ParticleReference knockParticles;
        [EditorTools.AberrationEndToolBar]
        #endregion

        #region References
        [EditorTools.AberrationToolBar("References")]
        public Canvas canvas;
        public TrailRenderer trail;
        public Events.StartFade fade;
        public LayerMask groundLayer;
        public SkinnedMeshRenderer[] characterShell;
        public Animator animator;
        [EditorTools.AberrationEndToolBar]
        #endregion

        #region Event Fields

        [EditorTools.AberrationToolBar("Events")]
        [Header("Audio")]
        public UnityEvent onStartCharge;
        public UnityEvent onCharge;
        public UnityEvent onHit;
        public UnityEvent onMove;
        public UnityEvent onDeath;
        [EditorTools.AberrationEndToolBar]

        #endregion

        public bool debug;
        [HideInInspector] public float tookDamageTimer = 0f;
        [HideInInspector] public bool hittingScreen = false;
        [HideInInspector] public InputDevice inputDevice;

        #region Privates
        private int _playerID; // Important.
        private Transform _transform;

        private Rigidbody _rigidBody;
        private PlayerInput _playerActions = null;
        private PlayerBase _lastPlayerHit = null;

        private Vector2 _stickDir;
        private Vector3 _currentChargeVelocity = Vector3.zero;
        private Vector3 _currentPos = Vector3.zero;

        private Vector3 _chargeDir;

        private bool _charging = false;
        private bool _started = false;

        private bool _isGrounded = false;

        private float _threshold = 0f;
        private float _chargeCooldown = 0f;
        private float _stunnedTime = 0f;
        private float _shockTrapTime = 0f; // Probably remove this?
        private float _chargeTime = 0f;
        private float _currentChargeRot = 0f;
        private float _chargeTimeSave = 0f;

        private Gamepad _currentGamePad;
        private float _vibrationDuration;
        private float _vibrationLow;
        private float _vibrationHigh;
        private bool _vibrationSmoothing;

        private Color _currentTrailCol;

        private System.Action<InputDevice, InputDeviceChange> _cachedHandler;
        #endregion

        public void Awake()
        {
            _rigidBody = GetComponent<Rigidbody>();
            _playerActions = GetComponent<PlayerInput>();
            _transform = GetComponent<Transform>();
        }

        /// <summary>
        /// Start, runs once before first frame
        /// </summary>
        [EditorTools.AberrationDescription("Load player data on start")]
        public override void Start()
        {
            base.Start();

            _started = true;
            _stunnedTime = stunnedTimer;
            _chargeTime = 0;
            _chargeTimeSave = 0;

            chargeParticles.Detach();
            stunParticles.Detach();
            knockParticles.Detach();
            splashParticles.Detach();

            EndCharge();
            stunParticles.Play(false);

            animator.SetBool("tookDmg", false);

            SetTrailSize(0, 0, 0);

            _currentTrailCol = Color.white;

            if (inputDevice != null)
                Remap(inputDevice);
        }

        [EditorTools.AberrationDescription("Simple external method for remapping", "Jacob Cooper", "16/11/2021")]
        public void Remap(InputDevice a_device)
        {

            _playerActions.SwitchCurrentControlScheme(a_device);
        }

        [EditorTools.AberrationDescription("Reconnecting the lost device", "Jacob Cooper", "16/11/2021")]
        public void OnDeviceReconnect(InputDevice a_device, InputDeviceChange a_inputChanged)
        {
            var data = Players.PlayerDictionary[_playerID];
            
            if (!data.disconnected ||
                a_inputChanged != InputDeviceChange.Reconnected ||
                a_device.deviceId != data.device.deviceId)
                return;

            InputSystem.onDeviceChange -= _cachedHandler;
            _cachedHandler = null;

            data.disconnected = false;
            data.device = a_device;

            Players.PlayerDictionary[_playerID] = data;

            if (Events.GameUILoad.Instance != null && Events.Round.Instance.IsInRound())
                Events.GameUILoad.Instance.PauseMenu(true, false);

            _playerActions.SwitchCurrentControlScheme(a_device);
        }

        [EditorTools.AberrationDescription("Setting up device lost events.", "Jacob Cooper", "16/11/2021")]
        public void OnDeviceLost(PlayerInput a_lost)
        {
            var data = Players.PlayerDictionary[_playerID];

            data.disconnected = true;

            Players.PlayerDictionary[_playerID] = data;

            _cachedHandler = (device, deviceChange) =>
            {
                OnDeviceReconnect(device, deviceChange);
            };

            InputSystem.onDeviceChange += _cachedHandler;

            if (Events.GameUILoad.Instance != null && Events.Round.Instance.IsInRound())
                Events.GameUILoad.Instance.PauseMenu(true, true);
        }

        [EditorTools.AberrationDescription("Change the players trail colour, used mainly for charging.", "Jacob Cooper", "14/11/2021")]
        public void SetTrailColour(Color a_col)
        {
            trail.material?.SetColor("Colour", new Color(a_col.r, a_col.g, a_col.b, 0.8f));
        }

        [EditorTools.AberrationDescription("Change the size of a players trail, used mainly for charging.", "Jacob Cooper", "14/11/2021")]
        public void SetTrailSize(float a_start, float a_finish, float a_delta)
        {
            trail.startWidth = a_delta == 0 ? a_start : Mathf.Lerp(trail.startWidth, a_start, a_delta);
            trail.endWidth = a_delta == 0 ? a_finish : Mathf.Lerp(trail.endWidth, a_finish, a_delta);
        }

        [EditorTools.AberrationDescription("Falling into water splash effect.", "Jacob Cooper", "14/11/2021")]
        public void Splash()
        {
            splashParticles.particleHolder.position = transform.position;
            splashParticles.particleHolder.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            splashParticles.Play(true, true);
        }

        [EditorTools.AberrationDescription("Modify the players flashing when low.", "Jacob Cooper", "14/11/2021")]
        public void SetFlashShader(Color a_color, float a_amount = 3f, float a_power = 5f, bool a_emissive = true)
        {
            foreach (var flash in characterShell)
            {
                if (flash.materials.Length > 1)
                {
                    flash.materials[1].SetInt("_emissionBool", a_emissive ? 1 : 0);
                    flash.materials[1].SetFloat("Speed", a_amount);
                    flash.materials[1].SetFloat("Power", a_power);
                    flash.materials[1].SetColor("Colour", a_color);
                }
            }
        }

        [EditorTools.AberrationDescription("Modify the players flashing when low.", "Jacob Cooper", "14/11/2021")]
        public void SetFlashShader(bool a_emissive)
        {
            foreach (var flash in characterShell)
            {
                if (flash.materials.Length > 1)
                {
                    flash.materials[1].SetInt("_emissionBool", a_emissive ? 1 : 0);
                }
            }
        }

        [EditorTools.AberrationDescription("Adds player threshold.", "Jacob Cooper", "3/11/2021")]
        public void AddThreshold(float a_threshold)
        {
            _threshold += a_threshold;
        }


        [EditorTools.AberrationDescription("Gets player threshold.", "Jacob Cooper", "3/11/2021")]
        public float GetThreshold()
        {
            return _threshold;
        }

        [EditorTools.AberrationDescription("Set the players index.", "Jacob Cooper", "2/11/2021")]
        public void SetPlayerIndex(int a_playerIndex)
        {
            _playerID = a_playerIndex;
        }

        [EditorTools.AberrationDescription("Get the player index.", "Jacob Cooper", "2/11/2021")]
        public int GetPlayerIndex()
        {
            return _playerID;
        }

        [EditorTools.AberrationDescription("Get the calculated float charge duration percentage.", "Jacob Cooper", "2/11/2021")]
        public float GetChargePercentageDuration()
        {
            return Mathf.Clamp((Time.realtimeSinceStartup - _chargeTime), 0, maxChargeTime) / maxChargeTime;
        }

        [EditorTools.AberrationDescription("Is the player grounded?", "Jacob Cooper", "14/11/2021")]
        public bool IsGrounded()
        {
            return _isGrounded;
        }

        /// <summary>
        /// Fixed update
        /// </summary>
        [EditorTools.AberrationDescription("Updates player physics on a fixed timestep", "Duncan Sykes", "21/10/2021")]
        public new void FixedUpdate()
        {
            base.FixedUpdate();

            // Threshold colouring
            if (_threshold > (maximumThreshold / 2))
            {
                //Color a = Color.white;
                //a.a = (_threshold / maximumThreshold);

                SetFlashShader(true);
            }
            else
                SetFlashShader(false);
            //

            if (Events.GameUILoad.Instance != null)
            {
                if (Players.PlayerDictionary[_playerID].dead)
                {
                    Events.GameUILoad.Instance.SetKnockThreshold(_playerID, 0);
                    Events.GameUILoad.Instance.SetCharge(_playerID, 0);

                    return;
                }
                
                Events.GameUILoad.Instance.SetKnockThreshold(_playerID, _threshold / maximumThreshold);

                if (_charging && playerState == ControlState.Enabled)
                {
                    Events.GameUILoad.Instance.SetCharge(_playerID, GetChargePercentageDuration());
                }
                else if (Events.GameUILoad.Instance != null)
                    Events.GameUILoad.Instance.SetCharge(_playerID, 0);
            }
        }

        public MeshRenderer testBlur;

        [EditorTools.AberrationDescription("Charging effects run in update.", "Jacob Cooper", "14/11/2021")]
        public void ChargeParticles(float a_rotDelta)
        {
            float dur = _charging ? GetChargePercentageDuration() : 0;
            float currentDur = _chargeTime > 0 ? (Time.realtimeSinceStartup - _chargeTime) : 0;

            bool isBurningOut = (dur >= (maxChargeTime/maxChargeBeforeStun));

            if (isBurningOut && currentDur >= maxChargeBeforeStun)
            {
                StunPlayer();

                return;
            }

            if (chargeParticles.particleHolder != null)
            {
                SetTrailColour(_currentTrailCol);
                SetTrailSize((dur * maxTrailSize / 2), dur * maxTrailSize, a_rotDelta * 10f);

                chargeParticles.particleHolder.position = transform.position + (Vector3.up * chargeParticles.up);

                // Scaling is cool :P

                if (_charging && playerState == ControlState.Enabled)
                {
                    float speed = a_rotDelta * acceleration;

                    //transform.rotation.eulerAngles;

                    // Gets facing away from stick dir.
                    Vector3 stickDir = new Vector3(_stickDir.x, 0, _stickDir.y);

                    _currentChargeRot += (dur * chargeRotMaxSpeed) * speed;

                    Quaternion dir = Quaternion.LookRotation(stickDir, Vector3.up);
                    dir *= Quaternion.Euler(Vector3.right * _currentChargeRot);

                    transform.rotation = Quaternion.Lerp(transform.rotation, dir, speed);

                    chargeParticles.particleHolder.transform.localScale = Vector3.one * (dur * maxChargeParticleScale);

                    // Face away from the direction we want to go.
                    chargeParticles.particleHolder.rotation = Quaternion.LookRotation(-stickDir, Vector3.up);

                    _currentTrailCol = Players.PlayerDictionary[_playerID].color; //Color.Lerp(_currentTrailCol, (isBurningOut ? burnOutColour : Players.PlayerDictionary[_playerID].color), a_rotDelta * 2f);

                    chargeParticles.SetMatOverrideColour((isBurningOut ? burnOutColour : normalChargeColour), true, a_rotDelta * 2f);
                }
                else
                    chargeParticles.SetMatOverrideColour(Color.clear);

                //float max = _charging ? 0.005f : 0f;
                //testBlur.material.SetFloat("_Amount", Mathf.Clamp(dur * max, 0, max));
            }
        }

        [EditorTools.AberrationDescription("Framed animation events.", "Jacob Cooper", "14/11/2021")]
        public void Animate()
        {
            if ((debug || Events.Round.IsIngame()) && !animator.GetBool("isGameStart"))
                animator.SetBool("isGameStart", true);

            if (animator.GetBool("tookDmg"))
            {
                if (Time.realtimeSinceStartup > tookDamageTimer)
                    animator.SetBool("tookDmg", false);
                else if (playerState != ControlState.Shocked)
                {
                    //transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
                }
            }

            animator.SetBool("isGround", _isGrounded);

            // Just make it so idle doesnt run whilst ingame
            animator.SetFloat("playerSpeed", (debug || Events.Round.IsIngame()) ? 
                Mathf.Clamp(_rigidBody.velocity.magnitude, 0.15f, _rigidBody.velocity.magnitude)
                : _rigidBody.velocity.magnitude);
        }

        [EditorTools.AberrationDescription("Update tick for controlled movememnt.", "Jacob Cooper", "14/11/2021")]
        public override void UpdateTick(float a_tickDelta)
        {
            base.UpdateTick(a_tickDelta);

            //Debug.Log(playerState); // Current player State
            if (!_started) return;

            if (sendIt)
            {
                _rigidBody.AddForce(_rigidBody.velocity.normalized, ForceMode.Impulse);

                return;
            }

            _isGrounded = Physics.Raycast(transform.position, -Vector3.up, 10, groundLayer);

            Animate();

            ChargeParticles(a_tickDelta);

            _threshold = Mathf.Clamp(_threshold - (thresholdDecrease * a_tickDelta), 0, maximumThreshold);

            Vector3 move; // Stores new movement values from input
            // Charge State machine
            switch (chargeState)
            {
                case State.Cooldown:
                    _chargeCooldown -= a_tickDelta;
                    if (_chargeCooldown <= 0)
                    {
                        chargeState = State.Ready;
                    }
                    break;
                case State.Ready:
                    break;
            }
            // Player State machine
            switch (playerState)
            {
                case ControlState.Enabled: // if controls are enabled
                    if (!_charging)
                    {
                        move.x = _stickDir.x;
                        move.z = _stickDir.y;
                        move.y = 0;
                        _rigidBody.AddForce(move * acceleration, ForceMode.Acceleration);

                        // plays sound if player is moving
                        if (_rigidBody.velocity.magnitude > 0.2f)
                        {
                            onMove.Invoke();
                        }
                    }
                    else
                    {
                        _rigidBody.AddForce(brakeFactor * -new Vector3(_rigidBody.velocity.x, 0, _rigidBody.velocity.z), ForceMode.Acceleration);
                        move.x = _stickDir.x;
                        move.z = _stickDir.y;
                        move.y = 0;
                    }
                    // Velocity Smoothing (really cursed)
                    if (_rigidBody.velocity.sqrMagnitude > maxSpeed)
                    {
                        _rigidBody.velocity *= movementSmoothing;
                    }
                    break;

                case ControlState.Disabled: // Controls disabled
                    move = Vector3.zero;
                    break;

                case ControlState.Frozen: // Ice Trap
                    move = Vector3.zero;
                    break;

                case ControlState.Shocked: // Shock Trap
                    _shockTrapTime -= a_tickDelta;
                    if (_shockTrapTime <= 0)
                    {
                        skidding = false;
                        _rigidBody.velocity = Vector3.zero;
                        _rigidBody.angularVelocity = Vector3.zero;
                        playerState = ControlState.Enabled;
                        move = Vector3.zero;

                        animator.SetBool("shockTrap", false);
                    }
                    // Velocity Smoothing
                    _rigidBody.velocity = Vector3.zero;

                    break;
                case ControlState.Stunned: // Momentary non movement after successful charge
                    stunnedTimer -= a_tickDelta;
                    if (stunnedTimer <= 0)
                    {
                        playerState = ControlState.Enabled;
                        stunParticles.Play(false, true);

                        if (Events.GameUILoad.Instance != null)
                            Events.GameUILoad.Instance.SetStunned(_playerID, false);
                    }
                    // Velocity Smoothing
                    if (_rigidBody.velocity.sqrMagnitude > maxSpeed)
                    {
                        _rigidBody.velocity *= movementSmoothing;
                    }
                    break;
            }

          
        }

        /// <summary>
        /// Returns the player rigidbody when called 
        /// </summary>
        /// <returns> Loaded player rigidbidy </return>
        [EditorTools.AberrationDescription("Returns player rigidbody when called")]
        public Rigidbody GetRigidBody()
        {
            return _rigidBody;
        }

        [EditorTools.AberrationDescription("Open pause menu.", "Jacob Cooper", "16/11/2021")]
        public void Pause(InputAction.CallbackContext a_context)
        {
            if (!_started || !a_context.performed || TickBase.IsPaused() || Events.GameUILoad.Instance == null || !Events.Round.IsIngame()) return;

            Events.GameUILoad.Instance.PauseMenu(true, false);
        }

        /// <summary>
        /// Movement callback for local input system")]
        /// </summary>
        /// <param name="a_context">Callback context for the input system</param>
        /// <returns>Null</returns>*
        [EditorTools.AberrationDescription("Movement callback for local input system")]
        public void Move(InputAction.CallbackContext a_context)
        {
            if (!_started) return;

            //if (playerState == ControlState.Frozen) return;
            //// Get the axis
            //if (playerState == ControlState.Enabled)
            //{
                Vector2 direction = a_context.ReadValue<Vector2>();
                _stickDir = direction;
            //}  
        }
        
        /// <summary>
        /// Moves player rigidbody in a specifc dierction by a specified amount of force. Used by trap mechanics
        /// </summary>
        /// <param name="a_normalisedDirection">
        /// <param name="a_forceAmount">
        /// <returns>Null</returns>
        [EditorTools.AberrationDescription("Apply a force in a specific direction to the rigidbody")]
        public void ApplyForceInDirection(Vector3 a_normalisedDirection, float a_forceAmount)
        {
            _rigidBody.velocity = Vector3.zero; 
            _rigidBody.AddForce(a_normalisedDirection * a_forceAmount, ForceMode.Impulse);
        }

        /// <summary>
        /// Deflect player back against its own velocity
        /// </summary>
        /// <param name="a_amount"></param>
        [EditorTools.AberrationDescription("Deflect back a player based on its velocity")]
        public void Deflect(float a_amount)
        {
            Vector3 savedVel = _rigidBody.velocity; 
            _rigidBody.velocity = Vector3.zero;

            _rigidBody.AddForce(-savedVel.normalized * a_amount, ForceMode.Impulse);
        }

        /// <summary>
        /// Movement for networked solution. Takes float arguments.
        /// </summary>
        /// <param name="a_N">North direction of movement. If negative player moves south</param>
        /// <param name="a_R">Right direction of movement. If negative player moves Left</param>
        [EditorTools.AberrationDescription("Movement for networked solution. Takes float arguments.")]
        public void Movement(float a_N, float a_R)
        {
            if (!_started) return;

            // Get axis
            if (playerState == ControlState.Enabled) // if player has control (not on ice trap)
            {
                _stickDir = new Vector2(a_R, a_N);
            }
        }

        /// <summary>
        /// Movement for networked solution. Takes a single Vector3 Argument.
        /// </summary>
        /// <param name="a_direction">Movement vector. Vec2(N,R). Where N = north, -N = south. R = right, -R = left</param>
        [EditorTools.AberrationDescription("Movement for networked solution. Takes a single Vector3 Argument.")]
        public void Movement(Vector2 a_direction)
        {
            if (!_started) return;
           

            // Get axis
            if (playerState == ControlState.Enabled) // if player has control (not on ice trap)
            {
                _stickDir = a_direction;
            }
        }


        public void StunPlayer(float a_stunTime = 0f)
        {
            playerState = ControlState.Stunned;
            stunnedTimer = a_stunTime < 1 ? _stunnedTime : a_stunTime;

            stunParticles.Play(true, true);

            // Quick zap with smoothing.
            SetControllerVibration(1, 1, bumpDuration, true);

            EndCharge();

            if (Events.GameUILoad.Instance != null)
                Events.GameUILoad.Instance.SetStunned(_playerID, true);
        }

        public void EndCharge()
        {
            _charging = false;

            chargeParticles.Play(false);
            chargeState = State.Cooldown;
            _chargeCooldown = cooldownTimer;
            _chargeTime = 0;

            onCharge?.Invoke();
        }

       /// <summary>
       /// Calls charge mechanic on player
       /// </summary>
       /// <param name="a_context"></param>
        [EditorTools.AberrationDescription("Calls charge mechanic for player movement")]
        public void Charge(InputAction.CallbackContext a_context)
        {
            if (!_started || !gameObject.scene.IsValid() || playerState == ControlState.Stunned || !(Events.Round.IsIngame() || debug))
            {
                if (_charging)
                    EndCharge();

                return;
            }

            if (_currentGamePad == null)
                _currentGamePad = GetGamepad();

            _charging = !a_context.canceled;

            if (_charging)
            {
                onStartCharge?.Invoke();

                chargeParticles.particleHolder.transform.localScale = Vector3.zero;
                _chargeTime = Time.realtimeSinceStartup;
            }

            if (_stickDir != Vector2.zero)
                _chargeDir = _stickDir;

            Vector3 move;
            move.x = _chargeDir.x;
            move.z = _chargeDir.y;
            move.y = 0;

            chargeParticles.Play(true);

            if (a_context.canceled && chargeState != State.Cooldown)
            {
                float charge = Mathf.Clamp(Time.realtimeSinceStartup - _chargeTime, 0, (float)a_context.duration);
                float chargePerc = Mathf.Clamp(charge, 0, maxChargeTime) / maxChargeTime;

                _rigidBody.AddForce(move * chargeForce * chargePerc, ForceMode.Impulse);
                _chargeTimeSave = Time.realtimeSinceStartup + chargePerc;

                EndCharge();
            }
        }

        public float GetSavedChargeTime()
        {
            return _chargeTimeSave;
        }

        /// <summary>
        /// Applies a fan like air force in a direction
        /// </summary>
        /// <param name="a_force"></param>
        /// <param name="a_directionNormal"></param>
        [EditorTools.AberrationDescription("Applies Effect for Fan Trap")]
        public void ApplyFanForce( float a_force,Vector3 a_directionNormal)
        {
            _rigidBody.AddForce(a_force * a_directionNormal, ForceMode.Impulse);
        }

        /// <summary>
        /// Disables input and Sets player velocity to zero over a set amount of time
        /// </summary>
        [EditorTools.AberrationDescription("Applies Effect for Shock Trap")]
        public void FreezePlayer(float a_time)
        {
            _rigidBody.velocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            _shockTrapTime = a_time;
            playerState = ControlState.Shocked;

            animator.SetBool("tookDmg", true);
            tookDamageTimer = Time.realtimeSinceStartup + a_time;
            animator.SetBool("shockTrap", true);
        }

        /// <summary>
        /// Returns the last player that this collided with
        /// </summary>
        /// <returns>Player Game Object</returns>
        [EditorTools.AberrationDescription("Returns the last player that was collided with")]
        public PlayerBase LastPlayerHit()
        {
            return _lastPlayerHit;
        }

        public bool IsBetween(float a_range, float a_mag, float a_mag2)
        {
            // 10 - 5 > 8 - 5
            return ((a_mag + a_range) >= (a_mag2 - a_range) ||
                (a_mag2 + a_range) >= (a_mag - a_range));
        }

        public bool IsMidCharge()
        {
            return Time.realtimeSinceStartup < GetSavedChargeTime();
        }

        public Gamepad GetGamepad()
        {
            return Gamepad.all.FirstOrDefault(g => _playerActions.devices.Any(d => d.deviceId == g.deviceId));
        }

        public void SetControllerVibration(float a_ampLow, float a_ampHigh, float a_duration, bool a_smoothing = false)
        {
            if (_currentGamePad == null)
                _currentGamePad = GetGamepad();

            if (_currentGamePad != null)
            {
                _vibrationLow = a_ampLow;
                _vibrationHigh = a_ampHigh;
                _vibrationDuration = Time.realtimeSinceStartup + a_duration;

                _vibrationSmoothing = a_smoothing;
            }
        }

        public void HitScreen()
        {
            hittingScreen = true;
            _started = false;

            playerState = ControlState.Disabled;

            Transform m = Camera.main.transform;

            transform.SetParent(Camera.main.transform);

            transform.position = m.position + (m.forward * 8f) + (m.up * 2f);

            Base.PlayerLoader.Instance.noiseCam.m_AmplitudeGain = Settings.Instance.settingsReference.deathCameraShakeIntensity * 3f;

            animator.SetBool("tookDmg", true);
            animator.SetFloat("playerSpeed", 1);

            _rigidBody.useGravity = false;
            _rigidBody.velocity = Vector3.zero;
        }

        public void MovePlayerOnScreen(float a_delta)
        {
            Transform m = Camera.main.transform;

            Vector3 goal = m.position + (m.forward * 5f) + (m.up * -5f);

            transform.position = Vector3.Lerp(transform.position, goal, a_delta);

            transform.rotation = Quaternion.LookRotation(m.position - transform.position, Vector3.up);
        }

        /// <summary>
        /// On collision callback
        /// </summary>
        /// <param name="a_collision"></param>
        [EditorTools.AberrationDescription("Triggers a collision callback on collision")]
        public new void OnCollisionEnter(Collision a_collision)
        {
            base.OnCollisionEnter(a_collision);

            // When YOU run into another player.

            if (a_collision.gameObject.CompareTag("User")
                && a_collision.gameObject.TryGetComponent(out _lastPlayerHit))
            {
                // Do knock particle stuff here.
                if (knockParticles.particleHolder != null)
                {
                    knockParticles.particleHolder.position = ((a_collision.transform.position + transform.position) / 2);

                    knockParticles.Play(true);
                }

                onHit?.Invoke();

                Rigidbody body = _lastPlayerHit.GetRigidBody();
                if (body == null)
                    return;

                if (_lastPlayerHit.playerState == ControlState.Shocked)
                {
                    _lastPlayerHit.playerState = ControlState.Enabled;
                    _shockTrapTime = 0;
                }

                Vector3 knockbackDirection = this.transform.position - _lastPlayerHit.transform.position;

                // If the player is mid charging, add force to another player otherwise normal knockback.
                bool isMidCharge = _lastPlayerHit.IsMidCharge();
                bool isLocalMidCharge = IsMidCharge();

                // Knockback is multiplied by threshold multiplier and percentile.
                float threshKnockMult = isLocalMidCharge ? (1 + (thresholdKnockbackMultiplier * (_lastPlayerHit.GetThreshold() / _lastPlayerHit.maximumThreshold))) : 1;
                float multiplier = knockbackAmount * threshKnockMult;

                float mag = body.velocity.magnitude;
                float localmag = _rigidBody.velocity.magnitude;

                float magPerc = Mathf.Clamp(mag / (_lastPlayerHit.maxSpeed / 2), 0.1f, 1);

                SetControllerVibration(bumpAmpLow * magPerc, bumpAmpHigh * magPerc, bumpDuration * magPerc, true);

                // YOUR threshold is applied and pushes YOU back.
                body.AddForce(-knockbackDirection.normalized * multiplier, ForceMode.Impulse);

                if (isLocalMidCharge && !isMidCharge || localmag > mag)
                {
                    // Animate the hit on the enemy
                    _lastPlayerHit.animator.SetBool("tookDmg", true);
                    _lastPlayerHit.tookDamageTimer = Time.realtimeSinceStartup + 0.5f;

                    // Threshold increase multiplied by threshold mag mult and max speed.
                    float threshMagMult = 1 + (thresholdMagnitudeMultiplier * (localmag / maxSpeed));
                    _lastPlayerHit.AddThreshold(thresholdIncrease * threshMagMult);

                    if (_lastPlayerHit.GetThreshold() >= _lastPlayerHit.maximumThreshold &&
                        Events.Round.Instance != null)
                    {
                        Events.Round.Instance.ThresholdReachedEvent(_lastPlayerHit, this, -knockbackDirection.normalized);
                    }
                }

                if (_charging)
                {
                    StunPlayer(_stunnedTime / chargeCancelStunPercent);

                    return;
                }
            }    
        }

        private new void Update()
        {
            if (hittingScreen)
                MovePlayerOnScreen(Time.deltaTime / 2f);

            if (stunParticles.particleHolder != null)
                stunParticles.particleHolder.position = transform.position + (Vector3.up * stunParticles.up);

            if (_currentGamePad != null )
            {
                if (_vibrationDuration > Time.realtimeSinceStartup)
                {
                    _currentGamePad.SetMotorSpeeds(_vibrationLow, _vibrationHigh);
                }
                else if (_charging && playerState == ControlState.Enabled)
                {
                    float ramp = GetChargePercentageDuration() * maxRumbleCharge;

                    // Simulated 2D + 1D axis rumble.
                    float upDown = Mathf.Abs(_stickDir.y);
                    float leftRamp = Mathf.Clamp(upDown - _stickDir.x, 0, 1);
                    float rightRamp = Mathf.Clamp(upDown + _stickDir.x, 0, 1);

                    if (leftRamp == 0 && rightRamp == 0)
                    {
                        leftRamp = 0.5f;
                        rightRamp = 0.5f;
                    }

                    leftRamp *= ramp;
                    rightRamp *= ramp;

                    _currentGamePad.SetMotorSpeeds(leftRamp, rightRamp);
                }
                else if (_vibrationSmoothing && _vibrationLow > 0 && _vibrationHigh > 0)
                {
                    _vibrationLow = Mathf.Lerp(_vibrationLow, 0, Time.deltaTime * rumbleSmoothing);
                    _vibrationHigh = Mathf.Lerp(_vibrationHigh, 0, Time.deltaTime * rumbleSmoothing);

                    _currentGamePad.SetMotorSpeeds(_vibrationLow, _vibrationHigh);
                }
                else
                    _currentGamePad.SetMotorSpeeds(0f, 0f);
            }
        }

        private IEnumerator SlowStart()
        {
            yield return new WaitForSeconds(0.5f);

            PlayerLoader.Instance.PlayerJoin(_playerActions);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (canvas != null)
                DestructionHeap.PrepareForDestruction(canvas.gameObject);

            if (_currentGamePad != null)
                _currentGamePad.SetMotorSpeeds(0f, 0f);

            if (stunParticles.particleHolder != null)
                DestructionHeap.PrepareForDestruction(stunParticles.particleHolder.gameObject);

            if (splashParticles.particleHolder != null)
                DestructionHeap.PrepareForDestruction(splashParticles.particleHolder.gameObject);

            if (chargeParticles.particleHolder != null)
                DestructionHeap.PrepareForDestruction(chargeParticles.particleHolder.gameObject);
        }

    }
}
