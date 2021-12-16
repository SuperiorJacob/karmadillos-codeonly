using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("Tar trap that slows player down.", "Duncan Sykes", "14/11/2021")]
    public class OilTrap : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        public float newAcceleration = 0.5f;
        public float brakeFactor;
        private float _storedAccSmooth = 0;
        private float _storedMaxSpeed = 0;
        private float _storedAccMax = 0;

        public UnityEvent onUpdate;
        public void OnAwake(MechanicLoader a_loader) {}

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader) { }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
        }

        public void OnUpdate(MechanicLoader a_loader) 
        {
            onUpdate.Invoke();
        }

        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {
                Rigidbody _rigidBody = ply.GetRigidBody();
                _rigidBody.AddForce(brakeFactor * -new Vector3(_rigidBody.velocity.x, 0, _rigidBody.velocity.z), ForceMode.Acceleration);
                
                _storedAccSmooth = ply.movementSmoothing;
                _storedAccMax = ply.acceleration;
                _storedMaxSpeed = ply.maxSpeed;
                
                ply.acceleration = newAcceleration;
                ply.movementSmoothing = 0.8f;
                
            }
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {
                //onTrapExit.Invoke();
                ply.movementSmoothing = _storedAccSmooth;
                ply.acceleration = _storedAccMax;
                ply.maxSpeed = _storedMaxSpeed;
            }
        }

        public void TriggerStay(MechanicLoader a_loader, Collider a_collider) 
        {
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {

                 
                 
            }
        }
    }
}
