using UnityEngine;

namespace AberrationGames.Interfaces
{
    /// <summary>
    /// Trigger interface used for classes that require event triggers.
    /// </summary>
    public interface ITrigger
    {
        public void TriggerEnter(Base.MechanicLoader a_loader, Collider a_collider);
        public void TriggerExit(Base.MechanicLoader a_loader, Collider a_collider);
        public void TriggerStay(Base.MechanicLoader a_loader, Collider a_collider);
    }
}
