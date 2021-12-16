using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("Shock trap that charges up and AOE stuns players.", "Jacob Cooper", "29/10/2021")]
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug)]
    public class ShockTrapVariant : MechanicBase, Interfaces.IMechanic
    {
        [EditorTools.AberrationToolBar("Trap Modifers")]
        public bool debug = false;

        [SerializeField] private Transform _scalier;
        [SerializeField, Range(0f, 100f)] private float _radius;
        [SerializeField, Range(0f, 100f)] private float _delay;
        [SerializeField, Range(0f, 100f)] private float _shockFinishDelay;
        [SerializeField, Range(0f, 100f)] private float _resetDelay;
        [SerializeField, Range(0f, 100f)] private float _shockTime;
        [SerializeField, Range(0f, 500f)] private float _shakeSpeed;
        [SerializeField, Range(0f, 500f)] private float _shakeAmount;
        [SerializeField, Range(-2f, 2f)] private float _upSpeed;
        [SerializeField, Range(-2f, 2f)] private float _downSpeed;
        [SerializeField, Range(1, 4)] private int _maxTargets = 4;
        [SerializeField] private int _maxUses = 0;
        [SerializeField] private Animator _animation;
        public UnityEvent onDischarge;

        [EditorTools.AberrationEndToolBar]

        [EditorTools.AberrationToolBar("Lightning")]

        [SerializeField, EditorTools.AberrationRequired] private Material _lightningMaterial;
        [SerializeField, EditorTools.AberrationRequired] private Transform _lightningEmissionPoint;
        [SerializeField, EditorTools.AberrationRequired] private int _arcAmount;
        [SerializeField, Range(1, 100)] private int _minArcs = 1;
        [SerializeField, Range(1, 100)] private int _maxArcs = 1;
        [SerializeField] private float _minArcHeight;
        [SerializeField] private float _maxArcHeight;

        [EditorTools.AberrationEndToolBar]

        private List<Base.PlayerBase> _targets;
        private List<GameObject> _playersInRange;
        private LineRenderer[] _lineRenderer;
        private GameObject[] _lightningObjects;

        private bool _triggered = false;
        private bool _ready = false;
        private bool _foundPlayers = false;
        private float _timer = 0;
        private float _scaleTimer = 0;
        private bool _exploded = false;
        private bool _resting = false;
        private float _useage = 0;

        public void OnAwake(MechanicLoader a_loader)
        {
        }

        public void Explode()
        {
            HideLightning(false);

            foreach (var player in _playersInRange)
            {
                if (player != null && player.TryGetComponent(out PlayerBase pBase))
                {
                    _targets.Add(pBase);

                    pBase.FreezePlayer(_shockTime);
                }
            }

            _useage++;
        }

        public void HideLightning(bool a_shouldHide)
        {
            foreach (var a in _lightningObjects)
            {
                if (a != null)
                    a.SetActive(!a_shouldHide);
            }
        }

        public void ResetTrap()
        {
            _animation.SetBool("Up", false);
            _animation.SetFloat("Speed", 1000f);
            _animation.Play("Animate");

            _animation.speed = 0;
            _timer = 0;
            _scaleTimer = 0;
            _triggered = false;
            _ready = false;
            _resting = false;
            _exploded = false;
            _foundPlayers = false;

            _scalier.localScale = Vector3.zero;

            _targets.Clear();

            HideLightning(true);
        }

        public void NewRoundResetTrap()
        {
            ResetTrap();

            _useage = 0;
        }

        public void TriggerTrap()
        {
            _triggered = true;

            _timer = Time.realtimeSinceStartup + _delay;
            _scaleTimer = Time.realtimeSinceStartup + _delay;
            _animation.speed = 1;
            _animation.SetBool("Up", true);
            _animation.SetFloat("Speed", _upSpeed);
            _animation.Play("Animate");

            _scalier.localScale = Vector3.zero;
        }

        public void OnFixedUpdate(MechanicLoader a_loader)
        {
            if (!_ready || !debug && !Events.Round.IsIngame())
                return;

            _foundPlayers = FindPlayersWithinRange();

            if (_foundPlayers && !_resting)
            {
                if (_triggered && !_exploded && _timer < Time.realtimeSinceStartup)
                {
                    _exploded = true;
                    _timer = Time.realtimeSinceStartup + _shockFinishDelay;

                    Explode();
                }
                else if (_triggered && _exploded && _timer < Time.realtimeSinceStartup)
                {
                    _animation.SetBool("Up", false);
                    _animation.SetFloat("Speed", _downSpeed);
                    _animation.Play("Animate");

                    _timer = Time.realtimeSinceStartup + _resetDelay;

                    _scaleTimer = Time.realtimeSinceStartup + _delay;
                    _resting = true;

                    HideLightning(true);
                }
                else if (!_triggered)
                {
                    TriggerTrap();
                }
            }
            else if (_resting && _timer < Time.realtimeSinceStartup)
            {
                ResetTrap();
            }
        }

        // Duncan <3
        public void CreateLightning()
        {
            onDischarge.Invoke();
            _lightningObjects = new GameObject[_arcAmount];
            _lineRenderer = new LineRenderer[_arcAmount];

            for (int i = 0; i < _arcAmount; i++)
            {
                GameObject lightningObject = new GameObject("LightningObject:" + i.ToString());
                lightningObject.transform.parent = this.gameObject.transform;

                var Lr = lightningObject.AddComponent<LineRenderer>();
                Lr.startWidth = 0.05f;
                Lr.endWidth = 0.05f;
                Lr.material = _lightningMaterial;
                _lineRenderer[i] = Lr;
                _lightningObjects[i] = lightningObject;

                lightningObject.SetActive(false);
            }
        }

        public void OnStart(MechanicLoader a_loader)
        {
            _ready = debug;

            _playersInRange = new List<GameObject>();
            _targets = new List<PlayerBase>();

            CreateLightning();

            _scalier.localScale = Vector3.zero;

            if (Events.Round.Instance != null)
                Events.Round.Instance.postEndRound.AddListener(NewRoundResetTrap);
        }

        public void MoveLightning()
        {
            int count = 0;
            int currentMax = _lineRenderer.Length / _targets.Count;

            foreach (var ply in _targets)
            {
                if (Vector3.Distance(ply.transform.position, transform.position) > _radius)
                    continue;

                ply.transform.rotation = Quaternion.LookRotation(ply.transform.forward, Vector3.up);

                //ply.transform.rotation = Quaternion.Euler(
                //        Mathf.Sin(Time.time * _shakeSpeed) * Random.Range(0, _shakeAmount),
                //        Mathf.Sin(Time.time * _shakeSpeed) * Random.Range(0, _shakeAmount),
                //        Mathf.Sin(Time.time * _shakeSpeed) * Random.Range(0, _shakeAmount)
                //    );

                // Duncan <3
                for (int line = count; line < currentMax; line++)
                {
                    LineRenderer lines = _lineRenderer[line];

                    if (lines == null)
                        continue;

                    int arcs = Random.Range(_minArcs, _maxArcs);
                    Vector3[] arcPositions = new Vector3[arcs + 2];
                    lines.positionCount = arcPositions.Length;
                    arcPositions[0] = _lightningEmissionPoint.position;
                    arcPositions[arcPositions.Length - 1] = ply.transform.position;

                    Vector3 positionDifference = ply.transform.position - _lightningEmissionPoint.position;


                    Vector3 directionToArc = positionDifference.normalized;

                    float length = positionDifference.magnitude;
                    positionDifference.y = 0;

                    float averageDistance = length / arcs;
                    float randomDistanceToAdd = averageDistance * 0.3f;

                    for (int i = 1; i < arcPositions.Length - 1; i++)
                    {
                        if (i == arcPositions.Length - 2)
                        {
                            arcPositions[i] = _lightningEmissionPoint.position +
                                              directionToArc * (averageDistance * i) +
                                              (Random.Range(-randomDistanceToAdd, 0) *
                                              directionToArc);
                        }
                        else
                        {
                            arcPositions[i] = _lightningEmissionPoint.position +
                                              directionToArc * (averageDistance * i) +
                                              (Random.Range(-randomDistanceToAdd, randomDistanceToAdd) *
                                              directionToArc);
                        }
                        arcPositions[i].y = Random.Range(_minArcHeight, _maxArcHeight);
                    }
                    lines.SetPositions(arcPositions);
                }

                count = currentMax;
                currentMax += _lineRenderer.Length / _targets.Count;
                currentMax = Mathf.Clamp(currentMax, 0, _lineRenderer.Length);
            }
        }

        public void OnUpdate(MechanicLoader a_loader)
        {
            if (_ready && _resting && _maxUses > 0 && _useage >= _maxUses)
            {
                _ready = false;

                ResetTrap();

                return;
            }

            if (!_ready)
            {
                _ready = debug || Events.Round.IsIngame();

                return;
            }
            else if (_triggered)
            {
                if (_exploded && !_resting)
                    MoveLightning();

                if (_scaleTimer > Time.realtimeSinceStartup)
                {
                    float rad = _radius * 2;

                    if (_resting)
                    {
                        Vector3 size = Vector3.one * Mathf.Clamp(rad * ((_scaleTimer - Time.realtimeSinceStartup) / _delay), 0, rad);

                        _scalier.localScale = Vector3.Lerp(_scalier.localScale, size, Time.fixedDeltaTime * 10);
                    }
                    else
                    {
                        Vector3 size = Vector3.one * Mathf.Clamp(rad * (1 - ((_scaleTimer - Time.realtimeSinceStartup)) / _delay), 0, rad);

                        _scalier.localScale = Vector3.Lerp(_scalier.localScale, size, Time.fixedDeltaTime * 10);
                    }
                }
            }
        }

        private bool FindPlayersWithinRange()
        {
            bool found = false;

            _playersInRange.Clear();
            foreach (var ply in Base.Players.PlayerDictionary)
            {
                if (_playersInRange.Count >= _maxTargets)
                    break;

                if (ply.Value.player != null)
                {
                    GameObject p = ply.Value.player;

                    float dist = Vector3.Distance(p.transform.position, transform.position);
                    if (dist < _radius)
                    {
                        _playersInRange.Add(p);
                        found = true;
                    }
                }
            }

            return found;
        }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {

        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color oldCol = Gizmos.color;

            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, _radius);

            Gizmos.color = oldCol;
        }
#endif
    }
}
