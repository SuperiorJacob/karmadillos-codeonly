using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Base
{
    /// <summary>
    /// To be implemented.
    /// </summary>
    /// 
    [EditorTools.AberrationDescription("Loads specific interface based mechanics into a loader and renders them.", "Jacob Cooper", "15/10/2021")]
    public class MechanicLoader : AberrationMonoBehaviour
    {
        // Very useful Unity Interface Support > https://github.com/TheDudeFromCI/Unity-Interface-Support <
        [SerializeField]
        private Mechanics.MechanicBase[] mechanics;

        public Structs.MechanicActions actions;

        private bool _loadedMechanics = false;
        private bool _collision = false;
        private bool _trigger = false;

        public override void Start()
        {
            base.Start();

            foreach (Mechanics.MechanicBase mechanic in mechanics)
            {
                LoadMechanic(mechanic);
            }

            if (!_loadedMechanics) return;

            actions.start.Invoke(this);
        }

        public void Update()
        {
            if (!_loadedMechanics) return;

            actions.update.Invoke(this);
        }

        public void FixedUpdate()
        {
            if (!_loadedMechanics) return;

            actions.fixedUpdate.Invoke(this);
        }

        public void OnCollisionEnter(Collision a_collision)
        {
            if (!_loadedMechanics || !_collision) return;

            actions.collisionEnter.Invoke(this, a_collision);
        }

        public void OnCollisionExit(Collision a_collision)
        {
            if (!_loadedMechanics || !_collision) return;

            actions.collisionExit.Invoke(this, a_collision);
        }

        public void OnCollisionStay(Collision a_collision)
        {
            if (!_loadedMechanics || !_collision) return;

            actions.collisionStay.Invoke(this, a_collision);
        }

        public void OnTriggerEnter(Collider a_collider)
        {
            if (!_loadedMechanics || !_trigger) return;

            actions.triggerEnter.Invoke(this, a_collider);
        }

        public void OnTriggerExit(Collider a_collider)
        {
            if (!_loadedMechanics || !_trigger) return;

            actions.triggerExit.Invoke(this, a_collider);
        }

        public void OnTriggerStay(Collider a_collider)
        {
            if (!_loadedMechanics || !_trigger) return;

            actions.triggerStay.Invoke(this, a_collider);
        }

        public override void UpdateTick(float a_tickDelta)
        {
            if (!_loadedMechanics) return;

            actions.tickUpdate.Invoke(this, a_tickDelta);
        }

        public void LoadMechanic(Mechanics.MechanicBase a_mechanic)
        {
            Interfaces.IMechanic mechanic = a_mechanic as Interfaces.IMechanic;
            if (mechanic == null) return;

            Debug.Log(mechanic);

            actions.start += mechanic.OnStart;
            actions.fixedUpdate += mechanic.OnFixedUpdate;
            actions.update += mechanic.OnUpdate;
            actions.awake += mechanic.OnAwake;
            actions.tickUpdate += mechanic.OnTickUpdate;
           
            Interfaces.ICollision collision = a_mechanic as Interfaces.ICollision;
            if (collision != null)
            {
                _collision = true;
                actions.collisionEnter += collision.CollisionEnter;
                actions.collisionExit += collision.CollisionExit;
                actions.collisionStay += collision.CollisionStay;
            }

            Interfaces.ITrigger trigger = a_mechanic as Interfaces.ITrigger;
            if (trigger != null)
            {
                _trigger = true;
                actions.triggerEnter += trigger.TriggerEnter;
                actions.triggerExit += trigger.TriggerExit;
                actions.triggerStay += trigger.TriggerStay;
            }

            _loadedMechanics = true;
        }
    }
}
