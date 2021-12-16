using UnityEngine;

namespace AberrationGames.Interfaces
{
    /// <summary>
    /// Physics interface used for classes that require collision events.
    /// </summary>
    public interface ICollision
    {
        public void CollisionEnter(Base.MechanicLoader a_loader, Collision a_collision);
        public void CollisionExit(Base.MechanicLoader a_loader, Collision a_collision);
        public void CollisionStay(Base.MechanicLoader a_loader, Collision a_collision);
    }
}
