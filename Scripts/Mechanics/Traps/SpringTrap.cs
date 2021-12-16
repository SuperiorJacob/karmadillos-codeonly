using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("Bounces player into the direction its facing.", "Duncan Sykes", "14/11/2021")]
    public class SpringTrap : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        
        public float springForce = 0.5f;
        public ForceMode forceMode = ForceMode.Impulse;

        public UnityEvent onDischarge;
        public UnityEvent onRecharge;
        public UnityEvent onReady;


        public void OnAwake(MechanicLoader a_loader) {}

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader) { }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
        }

        public void OnUpdate(MechanicLoader a_loader) { }

        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.TryGetComponent(out Rigidbody ply))
            {
                onDischarge.Invoke();
                ply.velocity = Vector3.zero;
                ply.AddForce(this.transform.up * springForce,forceMode);
            }
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
          
        }

        public void TriggerStay(MechanicLoader a_loader, Collider a_collider) { }
    }
}
