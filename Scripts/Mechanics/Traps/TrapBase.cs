using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("The base to all traps.", "Jacob Cooper", "14/11/2021")]
    public class TrapBase : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        
        public virtual void OnAwake(MechanicLoader a_loader) {}

        public virtual void OnFixedUpdate(MechanicLoader a_loader) { }

        public virtual void OnStart(MechanicLoader a_loader) { }

        public virtual void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
        }

        public virtual void OnUpdate(MechanicLoader a_loader) { }

        public virtual void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
        }

        public virtual void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
          
        }

        public virtual void TriggerStay(MechanicLoader a_loader, Collider a_collider) { }
    }
}
