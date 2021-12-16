using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("Launches the player up.", "Duncan Sykes", "14/11/2021")]
    public class LaunchPad : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {   
        public float padForce = 0.5f;

        public void OnAwake(MechanicLoader a_loader) { }

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader) { }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
        }

        public void OnUpdate(MechanicLoader a_loader) { }

        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {
                //left
                ply.ApplyForceInDirection(transform.up, padForce);
            }
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
        }

        public void TriggerStay(MechanicLoader a_loader, Collider a_collider) { }
    }
}
