using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Mechanics
{
    [EditorTools.AberrationDescription("Shock trap variant 2.", "Jacob Cooper", "14/11/2021")]
    public class ShockTrap_Variant2 : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        public UnityEngine.Events.UnityEvent onDischarge;
        public Animator animator;
        public Transform topper;
        public Transform trap;
        public LineRenderer lineRender;
        public BoxCollider boxCollider;

        public float triggerTimer = 5f;
        public float maxSize = 5f;
        public float sizeScaleSpeed = 10f;
        public float zapTime = 3f;
        public float beforeZap = 1f;

        public float slowSpeed = 10f;

        [Header("Connection")]
        public Vector3 inactiveCenter;
        public Vector3 inactiveSize;
        public Vector3 activeCenter;
        public Vector3 activeSize;
        public float connectionRange = 5f;

        [HideInInspector] public ShockTrap_Variant2 connection;
        [HideInInspector] public Transform connectionTopper;
        [HideInInspector] public Transform connectionTrap;
        [HideInInspector] public bool master = false;
        [HideInInspector] public bool solo = true;
        [HideInInspector] public bool connectionOperation = false;

        [SerializeField, Range(-2f, 2f)] private float _upSpeed;
        [SerializeField, Range(-2f, 2f)] private float _downSpeed;

        private bool _triggered = false;
        private float _triggerTime;
        private float _beforeZapTime;

        private float _afterZapTime;
        private bool _zapping = false;

        private PlayerBase _target;

        public void FindConnections()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, connectionRange);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.tag == "Trap" && 
                    hitCollider.transform.TryGetComponent(out ShockTrap_Variant2 trap) &&
                    trap != this && trap.connection == null)
                {
                    connection = trap;
                    connectionTopper = connection.topper;
                    connectionTrap = connection.trap;

                    connection.FindConnections();
                    connection.connectionOperation = false;

                    trap.master = false;
                    master = true;

                    trap.solo = false;
                    solo = false;

                    break;
                }
            }

            SetTriggerSpot();
        }

        public void SetTriggerSpot()
        {
            if (connection == null)
            {
                boxCollider.size = inactiveSize;
                boxCollider.center = inactiveCenter;
            }
            else
            {
                activeSize.z = Vector3.Distance(transform.position, connectionTrap.position);
                activeCenter.z = -1 * (activeSize.z / 2);

                boxCollider.size = activeSize;
                boxCollider.center = activeCenter;

                transform.rotation = Quaternion.LookRotation(transform.position - connectionTrap.position, Vector3.up);
            }
        }

        public void ResetTrap(bool a_hardReset = true)
        {
            _triggerTime = 0;
            _triggered = false;
            _target = null;
            _zapping = false;
            _triggerTime = Time.realtimeSinceStartup + triggerTimer;

            if (a_hardReset)
            {
                master = true;
                connection = null;
                connectionTopper = topper;
                connectionTrap = trap;

                FindConnections();
            }

            TriggerAnim(false);

            lineRender.startWidth = 0;

            if (connection == null)
                lineRender.SetPositions(
                    new Vector3[] {
                        topper.position,
                        topper.position,
                        topper.position
                    });
        }

        public void TriggerTrap()
        {
            _triggered = true;
            _target = null;
            lineRender.enabled = true;
            lineRender.startWidth = 0;
            lineRender.endWidth = 0;

            TriggerAnim(true);
            
            if (connection != null && master)
                connection.connectionOperation = true;

            lineRender.SetPositions(
                new Vector3[] {
                    topper.position,
                    (connectionTopper.position + topper.position)/2,
                    connectionTopper.position
                });
        }

        public void OnAwake(MechanicLoader a_loader) { }

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader)
        {
            lineRender.startWidth = 0;
            lineRender.endWidth = 0;
           
            ResetTrap();

            if (Events.Round.Instance != null)
                Events.Round.Instance.postStartRound.AddListener(ResetTrapRound);
        }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {

        }

        public void TriggerAnim(bool a_up, bool a_override = false)
        {
            if (connectionOperation && !a_override)
                return;

            animator.SetBool("Up", a_up);
            animator.SetFloat("Speed", a_up ? _upSpeed : _downSpeed);
            animator.Play("Animate");

            if (master && connection != null)
                connection.TriggerAnim(a_up);
        }
        
        public void OnUpdate(MechanicLoader a_loader)
        {
            if (!master)
                return;

            if (_triggerTime > 0 && _triggerTime < Time.realtimeSinceStartup && !_triggered)
            {
                TriggerTrap();
            }

            if (_triggered)
            {
                lineRender.startWidth = Mathf.Lerp(lineRender.startWidth, maxSize * (solo && _target == null ? 10 : 1), TickBase.deltaTime * sizeScaleSpeed);
                lineRender.endWidth = lineRender.startWidth;

                if (_target != null)
                {
                    if (_zapping && _afterZapTime < Time.realtimeSinceStartup)
                    {
                        _target = null;

                        ResetTrap(false);

                        return;
                    }

                    if (!_zapping && _beforeZapTime < Time.realtimeSinceStartup)
                    {
                        ZapTarget();
                        _zapping = true;
                        _afterZapTime = Time.realtimeSinceStartup + zapTime;

                    }
                    else
                    {
                        _target.GetRigidBody().velocity = Vector3.Lerp(_target.GetRigidBody().velocity, Vector3.zero, TickBase.deltaTime * slowSpeed);

                        if (connection == null)
                        {

                            lineRender.SetPositions(
                                new Vector3[] {
                                    topper.position,
                                    (topper.position + _target.transform.position) / 2,
                                    _target.transform.position
                                });
                        }
                        else
                            lineRender.SetPositions(
                                new Vector3[] {
                                    topper.position,
                                    _target.transform.position,
                                    connectionTopper.position
                                });
                    }

                }
                else
                    lineRender.SetPositions(
                        new Vector3[] {
                            topper.position,
                            (topper.position + connectionTopper.position)/2,
                            connectionTopper.position
                        });
            }
            else if (lineRender.startWidth > 0)
            {
                lineRender.startWidth = Mathf.Lerp(lineRender.startWidth, 0, TickBase.deltaTime * sizeScaleSpeed);
                lineRender.endWidth = lineRender.startWidth;

                lineRender.SetPositions(
                    new Vector3[] {
                        topper.position,
                        Vector3.Lerp(lineRender.GetPosition(1), (topper.position + connectionTopper.position)/2, TickBase.deltaTime * sizeScaleSpeed),
                        connectionTopper.position
                    });
            }
        }

        public void ZapTarget()
        {
            if (_target == null)
                return;

            _target.transform.rotation = Quaternion.LookRotation(_target.transform.forward, Vector3.up);
            _target.FreezePlayer(zapTime);
        }

        public void ApplyStun(GameObject a_player)
        {
            if (a_player.TryGetComponent(out PlayerBase player))
            {
                _beforeZapTime = Time.realtimeSinceStartup + beforeZap;
                _target = player;

                onDischarge.Invoke();
            }
        }

        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (_triggered && a_collider.tag == "User" && _target == null && master)
            {
                ApplyStun(a_collider.gameObject);
            }
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {

        }

        public void TriggerStay(MechanicLoader a_loader, Collider a_collider)
        {

        }

        private void ResetTrapRound()
        {
            ResetTrap();
        }
    }
}
