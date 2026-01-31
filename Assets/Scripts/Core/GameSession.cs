public class GameSession
{
    public CreepStore CreepStore { get; }

    public GameSession()
    {
        CreepStore = new CreepStore();
    }

    public void BeginFrame()
    {
        CreepStore.BeginFrame();
    }

    public void Reset()
    {
        CreepStore.Reset();
    }
}
