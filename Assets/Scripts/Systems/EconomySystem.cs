using System;

// Phase 3 resolution: applies buffered coin credits from creep kills, deducts turret costs.
// Single writer for EconomyStore. Subscribes to DamageSystem.OnCreepKilled (buffers locally).
public class EconomySystem : IGameSystem
{
    private readonly EconomyStore economyStore;
    private readonly TurretStore turretStore;
    private readonly int turretCost;

    private int pendingCoinCredits;

    public EconomySystem(EconomyStore economyStore, TurretStore turretStore, int turretCost)
    {
        this.economyStore = economyStore ?? throw new ArgumentNullException(nameof(economyStore));
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.turretCost = turretCost;
    }

    public void HandleCreepKilled(int creepId, int coinReward)
    {
        pendingCoinCredits += coinReward;
    }

    public void Tick(float deltaTime)
    {
        if (pendingCoinCredits > 0)
        {
            economyStore.AddCoins(pendingCoinCredits);
            pendingCoinCredits = 0;
        }

        var placed = turretStore.PlacedThisFrame;
        for (int i = 0; i < placed.Count; i++)
        {
            economyStore.TrySpendCoins(turretCost);
        }
    }

    public void Reset()
    {
        pendingCoinCredits = 0;
    }
}
