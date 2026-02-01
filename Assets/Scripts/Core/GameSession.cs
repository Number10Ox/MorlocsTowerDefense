public class GameSession
{
    public CreepStore CreepStore { get; }
    public BaseStore BaseStore { get; }

    public GameSession(int baseMaxHealth)
    {
        CreepStore = new CreepStore();
        BaseStore = new BaseStore(baseMaxHealth);
    }

    public void BeginFrame()
    {
        CreepStore.BeginFrame();
        BaseStore.BeginFrame();
    }

    public void Reset()
    {
        CreepStore.Reset();
        BaseStore.Reset();
    }
}
