using System;
using System.Collections.Generic;
using UnityEngine;

// Phase 3 resolution: applies buffered coin credits from creep kills, deducts turret costs from stats.
// Single writer for EconomyStore. Subscribes to DamageSystem.OnCreepKilled (buffers locally).
// Cost is read from statsByType — single source of truth shared with PlacementSystem.
public class EconomySystem : IGameSystem
{
    private readonly EconomyStore economyStore;
    private readonly TurretStore turretStore;
    private readonly IReadOnlyDictionary<TurretType, TurretTypeStats> statsByType;

    private int pendingCoinCredits;

    public EconomySystem(
        EconomyStore economyStore,
        TurretStore turretStore,
        IReadOnlyDictionary<TurretType, TurretTypeStats> statsByType)
    {
        this.economyStore = economyStore ?? throw new ArgumentNullException(nameof(economyStore));
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.statsByType = statsByType ?? throw new ArgumentNullException(nameof(statsByType));
    }

    public void HandleCreepKilled(int creepId, int coinReward)
    {
        if (coinReward <= 0) return;
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
            if (statsByType.TryGetValue(placed[i].Type, out TurretTypeStats stats))
            {
                economyStore.TrySpendCoins(stats.Cost);
            }
            else
            {
                Debug.LogWarning($"EconomySystem: No stats for TurretType {placed[i].Type}. Skipping cost deduction.");
            }
        }
    }

    public void Reset()
    {
        pendingCoinCredits = 0;
    }
}
