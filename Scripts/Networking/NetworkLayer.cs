namespace AberrationGames.Networking
{
    [System.Serializable]
    public enum NetworkLayer : ushort
    {
        Client = 0,
        LocalClient = 1,
        Master = 2
    }

    [System.Serializable]
    public enum NetworkTags : ushort
    {
        SpawnPlayer = 0,
        DespawnSplayer = 1,
        Movement = 2,
        Event = 3
    }

    [System.Serializable]
    public enum NetworkTypes : ushort
    {
        Scene = 0,
        Rigidbody = 1,
        Particles = 2,
    }
}
