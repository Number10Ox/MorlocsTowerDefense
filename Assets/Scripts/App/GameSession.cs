// Owns all stores. Coordinates per-frame BeginFrame (flush deferred removals) and full reset.
public class GameSession
{
    public CreepStore CreepStore { get; }
    public BaseStore BaseStore { get; }
    public TurretStore TurretStore { get; }
    public ProjectileStore ProjectileStore { get; }
    public EconomyStore EconomyStore { get; }
    public TurretSelectionStore TurretSelectionStore { get; }

    public GameSession(int baseMaxHealth, int startingCoins, TurretType defaultTurretType)
    {
        CreepStore = new CreepStore();
        BaseStore = new BaseStore(baseMaxHealth);
        TurretStore = new TurretStore();
        ProjectileStore = new ProjectileStore();
        EconomyStore = new EconomyStore(startingCoins);
        TurretSelectionStore = new TurretSelectionStore(defaultTurretType);
    }

    public void BeginFrame()
    {
        CreepStore.BeginFrame();
        BaseStore.BeginFrame();
        TurretStore.BeginFrame();
        ProjectileStore.BeginFrame();
        EconomyStore.BeginFrame();
    }

    public void Reset()
    {
        CreepStore.Reset();
        BaseStore.Reset();
        TurretStore.Reset();
        ProjectileStore.Reset();
        EconomyStore.Reset();
        TurretSelectionStore.Reset();
    }
}
