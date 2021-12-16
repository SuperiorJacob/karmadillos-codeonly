using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("A bouncy bumper that pushes players back.", "Jacob Cooper", "14/11/2021")]
    public class BumperTrap : MechanicBase, Interfaces.IMechanic, Interfaces.ICollision
    {
        public float bumperForce = 0.5f;

        public UnityEvent onTrigger;

        public void OnAwake(MechanicLoader a_loader) {}

        public void OnFixedUpdate(MechanicLoader a_loader) { }

        public void OnStart(MechanicLoader a_loader) { }

        public void OnUpdate(MechanicLoader a_loader) { }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
            
        }

        public void CollisionEnter(MechanicLoader a_loader, Collision a_collision)
        {
            if (a_collision.gameObject.TryGetComponent(out PlayerBase ply))
            {
                onTrigger.Invoke();
                //left
                Vector3 reflect = Vector3.Reflect(ply.GetRigidBody().velocity, a_collision.contacts[0].normal);
                ply.GetRigidBody().velocity = reflect * bumperForce;
            }
        }

        public void CollisionExit(MechanicLoader a_loader, Collision a_collision)
        {
        }

        public void CollisionStay(MechanicLoader a_loader, Collision a_collision)
        {
        }
    }
}
