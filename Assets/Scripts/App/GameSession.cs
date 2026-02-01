// Owns all stores. Coordinates per-frame BeginFrame (flush deferred removals) and full reset.
public class GameSession
{
    public CreepStore CreepStore { get; }
    public BaseStore BaseStore { get; }
    public TurretStore TurretStore { get; }
    public ProjectileStore ProjectileStore { get; }

    public GameSession(int baseMaxHealth)
    {
        CreepStore = new CreepStore();
        BaseStore = new BaseStore(baseMaxHealth);
        TurretStore = new TurretStore();
        ProjectileStore = new ProjectileStore();
    }

    public void BeginFrame()
    {
        CreepStore.BeginFrame();
        BaseStore.BeginFrame();
        TurretStore.BeginFrame();
        ProjectileStore.BeginFrame();
    }

    public void Reset()
    {
        CreepStore.Reset();
        BaseStore.Reset();
        TurretStore.Reset();
        ProjectileStore.Reset();
    }
}
