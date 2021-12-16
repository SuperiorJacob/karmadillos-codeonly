using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("A fan that blows players away.", "Duncan Sykes", "14/11/2021")]
    public class FanTrap : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        public UnityEvent onTrapStart;
        public UnityEvent onTrapEnd;
        
        [Range(0.000f,0.9999f)] public float fanForce = 0.5f;

        public void OnAwake(MechanicLoader a_loader) { }

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader) { }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
        }

        public void OnUpdate(MechanicLoader a_loader)
        {
            
;       }
        
        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {
                onTrapStart.Invoke();
                ply.ApplyFanForce(fanForce, transform.forward);
                //left
            }
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
        }

        public void TriggerStay(MechanicLoader a_loader, Collider a_collider) 
        {
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {
                //left
                onTrapEnd.Invoke();
                ply.ApplyFanForce(fanForce * 3, transform.forward);
            }
        }
    }
}
