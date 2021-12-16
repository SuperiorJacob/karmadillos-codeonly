namespace AberrationGames.Interfaces
{
    /// <summary>
    /// Menu interface for menu interactions like buttons.
    /// </summary>
    public interface IMenuInteraction
    {
        public void SetUIClick(Base.MainMenuPlayer a_player);
        public void SecondUIClick(Base.MainMenuPlayer a_player);
        public void ReleaseUIClick(Base.MainMenuPlayer a_player);
        public void ReloadClick(Base.MainMenuPlayer a_player, float a_reloadAmount = 1f);
    }
}
