namespace AberrationGames.Interfaces
{
    /// <summary>
    /// Mechanic interface for reference and method loading.
    /// </summary>
    public interface IMechanic
    {
        public void OnStart(Base.MechanicLoader a_loader);
        public void OnAwake(Base.MechanicLoader a_loader);
        public void OnUpdate(Base.MechanicLoader a_loader);
        public void OnFixedUpdate(Base.MechanicLoader a_loader);
        public void OnTickUpdate(Base.MechanicLoader a_loader, float a_tickDelta);
    }
}
