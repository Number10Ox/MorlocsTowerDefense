public class GameSession
{
    public CreepStore CreepStore { get; }
    public BaseStore BaseStore { get; }
    public TurretStore TurretStore { get; }

    public GameSession(int baseMaxHealth)
    {
        CreepStore = new CreepStore();
        BaseStore = new BaseStore(baseMaxHealth);
        TurretStore = new TurretStore();
    }

    public void BeginFrame()
    {
        CreepStore.BeginFrame();
        BaseStore.BeginFrame();
        TurretStore.BeginFrame();
    }

    public void Reset()
    {
        CreepStore.Reset();
        BaseStore.Reset();
        TurretStore.Reset();
    }
}
