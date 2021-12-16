using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug),
        EditorTools.AberrationDescription("Harpoon trap.", "Jacob Cooper", "21/10/2021")]
    public class HarpoonGun : TrapBase
    {
        [EditorTools.AberrationToolBar("Trap Modifers")]

        public bool debug = false;

        [SerializeField, Range(0f, 100f)] private float _distance;
        [SerializeField, Range(0f, 100f)] private float _unhookDistance = 0.1f;
        [SerializeField, Range(0f, 100f)] private float _pullForce;
        [SerializeField, Range(0f, 100f)] private float _grappleSpeed;
        [SerializeField, Range(0f, 360f)] private float _grappleFOV;
        [SerializeField, Range(0f, 100f)] private float _shootDelay;
        [SerializeField, Range(0f, 100f)] private float _rotationSpeed;
        [SerializeField] private Transform _rotator;
        [SerializeField] private Transform _grapple;
        [SerializeField] private Transform _grappleOrigin;
        public UnityEvent onFire;
        public UnityEvent onRetract;

        [EditorTools.AberrationEndToolBar]

        [EditorTools.AberrationToolBar("Line Renderer")]

        [SerializeField] private LineRenderer _lineRenderer;

        [EditorTools.AberrationEndToolBar]

        private GameObject _target;
        private Rigidbody _targetRB;

        private bool _targetWithinFOV = false;
        private bool _ready = false;
        private bool _fired = false;
        private float _shotTime = 0;
        private bool _retracting = false;
        private bool _tooClose = false;
        private bool _grabbed = false;

        public override void OnAwake(MechanicLoader a_loader) { }

        public override void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
            if (!_ready) return;

            if (_fired && (_grabbed || _shotTime < Time.realtimeSinceStartup))
            {
                Retract();
            }
        }

        public override void OnFixedUpdate(MechanicLoader a_loader) 
        {
            if (!_ready)
            {
                _target = null;
                ResetTrap();

                return;
            }

            if (!_retracting)
            {
                FindPlayerWithinRange();

                _targetWithinFOV = TargetWithinFOV(_target);
            }
        }

        public void ResetTrap()
        {
            _ready = false;
            _fired = false;
            _retracting = false;
            _grabbed = false;

            _lineRenderer.enabled = false;

            _grapple.position = _grappleOrigin.position;
            _grapple.rotation = _grappleOrigin.rotation;
        }

        public override void OnStart(MechanicLoader a_loader) 
        {
            _ready = debug;

            ResetTrap();

            if (Events.Round.Instance != null)
                Events.Round.Instance.postEndRound.AddListener(ResetTrap);
        }

        public override void OnUpdate(MechanicLoader a_loader)
        {
            if (!_ready)
            {
                _ready = debug || Events.Round.IsIngame();

                return;
            }

            _lineRenderer.SetPositions(new Vector3[] {
                _grappleOrigin.position,
                _grapple.position
            });

            _tooClose = (Vector3.Distance(_grapple.position, _grappleOrigin.position) < 0.5);

            if (_fired)
            {
                _lineRenderer.enabled = true;

                if (_grabbed || _shotTime < Time.realtimeSinceStartup || _target == null)
                    RetractSmooth();
                else if (_target != null)
                    ShootSmooth();
            }

            if (_target == null) 
            {
                RetractSmooth(true);

                return;
            }

            Quaternion lookOnLook = Quaternion.LookRotation((_target.transform.position - (Vector3.up / 2)) - _rotator.position);
            _rotator.rotation = Quaternion.Slerp(_rotator.rotation, lookOnLook, TickBase.deltaTime * _rotationSpeed);

            if (!_tooClose)
            {
                Quaternion grappleLook = Quaternion.LookRotation(_target.transform.position - _grapple.position);
                _grapple.rotation = grappleLook;
            }
            else
                _grapple.rotation = Quaternion.Slerp(_rotator.rotation, _grappleOrigin.rotation, TickBase.deltaTime * _rotationSpeed);

            if (_targetWithinFOV && !_fired && _shotTime < Time.realtimeSinceStartup)
                Shoot();
        }

        private void Shoot()
        {
            onFire.Invoke();
            _fired = true;
            _shotTime = Time.realtimeSinceStartup + _shootDelay;
            _targetRB = _target.GetComponent<Rigidbody>();
        }

        private void ShootSmooth()
        {
            Vector3 dir = (_target.transform.position - _grapple.position).normalized;
            bool tooClose = (Vector3.Distance(_grapple.position, _target.transform.position) < 0.2);

            if (!tooClose)
                _grapple.position += dir * TickBase.deltaTime * _grappleSpeed;
            else
            {
                Vector3 jitterGap = _tooClose ? Vector3.zero : (_grappleOrigin.position - _grapple.position).normalized / 2;
                _grapple.position = _target.transform.position + jitterGap;

                _grabbed = true;
            }
        }

        private void RetractSmooth(bool a_ignore = false)
        {
            if (_target == null || !_grabbed)
            {
                if (!a_ignore)
                    _retracting = true;

                _grapple.position = Vector3.Lerp(_grapple.position, _grappleOrigin.position, TickBase.deltaTime * _grappleSpeed);

                if (a_ignore && _tooClose)
                    ResetTrap();
            }
            else
            {
                Vector3 jitterGap = (_grappleOrigin.position - _grapple.position).normalized / 2;
                _grapple.position = _target.transform.position + jitterGap;
            }
        }

        private void Retract()
        {
            onRetract.Invoke();
            if (Vector3.Distance(_grapple.position, _grappleOrigin.position) < _unhookDistance)
            {
                _fired = false;
                _retracting = false;
                _grabbed = false;

                _shotTime = Time.realtimeSinceStartup + _shootDelay;

                ResetTrap();
            }
            else if (_target != null && _grabbed)
            {
                Vector3 jitterGap = (_grappleOrigin.position - _grapple.position).normalized;
                //_grapple.position = _target.transform.position + jitterGap;

                if (_targetRB != null)
                    _targetRB.AddForce(jitterGap * _pullForce, ForceMode.VelocityChange);
            }
        }

        private bool TargetWithinFOV(GameObject a_target = null)
        {
            if (a_target == null)
                return false;


            Vector3 toTarget = (_rotator.position - a_target.transform.position).normalized;

            return (Vector3.Angle(-transform.forward, toTarget) < _grappleFOV / 2);
        }

        private bool FindPlayerWithinRange()
        {
            bool found = false;
            float distance = _distance;
            GameObject target = null;

            if (_fired && _target && Vector3.Distance(_target.transform.position, transform.position) < _distance && TargetWithinFOV(_target))
                target = _target;
            else
                foreach (var ply in Base.Players.PlayerDictionary)
                {
                    if (ply.Value.player != null)
                    {
                        GameObject p = ply.Value.player;

                        float dist = Vector3.Distance(p.transform.position, transform.position);
                        if (dist < _distance && dist < distance && TargetWithinFOV(p))
                        {
                            distance = dist;
                            target = p;
                            found = true;
                        }
                    }
                }

            _target = target;

            return found;
        }

        private Vector3 DirFromAngle(float a_angleInDegrees, bool a_angleIsGlobal)
        {
            if (!a_angleIsGlobal)
            {
                a_angleInDegrees += transform.eulerAngles.y;
            }

            return new Vector3(Mathf.Sin(a_angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(a_angleInDegrees * Mathf.Deg2Rad));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color oldCol = Gizmos.color;

            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, _distance);

            Gizmos.color = _targetWithinFOV ? Color.red : Color.green;
            if (_target != null)
                Gizmos.DrawLine(_rotator.position, _target.transform.position);

            Gizmos.color = oldCol;

            // FOV
            Vector3 viewAngleA = DirFromAngle(-_grappleFOV / 2, false);
            Vector3 viewAngleB = DirFromAngle(_grappleFOV / 2, false);

            Handles.color = Color.white;
            Handles.DrawLine(transform.position, transform.position + viewAngleA * _distance);
            Handles.DrawLine(transform.position, transform.position + viewAngleB * _distance);
            //
        }
#endif
    }
}
