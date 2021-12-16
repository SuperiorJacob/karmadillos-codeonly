using UnityEngine;
using UnityEngine.Events;

namespace AberrationGames.Structs
{
    public struct MechanicActions
    {
        public UnityAction<Base.MechanicLoader> start;
        public UnityAction<Base.MechanicLoader> awake;
        public UnityAction<Base.MechanicLoader> update;
        public UnityAction<Base.MechanicLoader> fixedUpdate;
        public UnityAction<Base.MechanicLoader, float> tickUpdate;

        public UnityAction<Base.MechanicLoader, Collision> collisionEnter;
        public UnityAction<Base.MechanicLoader, Collision> collisionExit;
        public UnityAction<Base.MechanicLoader, Collision> collisionStay;

        public UnityAction<Base.MechanicLoader, Collider> triggerEnter;
        public UnityAction<Base.MechanicLoader, Collider> triggerExit;
        public UnityAction<Base.MechanicLoader, Collider> triggerStay;
    }
}
