using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("A slow trap that disables player input.", "Duncan Sykes", "14/11/2021")]
    public class IceTrap : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        public float speedMult = 1.5f;
        public UnityEvent playerTrigger;

        public void OnAwake(MechanicLoader a_loader) { }

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader) { }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
        }

        public void OnUpdate(MechanicLoader a_loader) { }

        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.transform.TryGetComponent(out PlayerBase ply))
            {
                playerTrigger.Invoke();
                ply.playerState = PlayerBase.ControlState.Frozen;
               // ply.GetRigidBody().AddForce(ply.GetRigidBody().velocity*speedMult, ForceMode.Acceleration);
            }
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
            if (a_collider.transform.TryGetComponent(out PlayerBase ply))
            {
                ply.playerState = PlayerBase.ControlState.Enabled;
                
            }
        }

         public void TriggerStay(MechanicLoader a_loader, Collider a_collider) { }
    }
}
