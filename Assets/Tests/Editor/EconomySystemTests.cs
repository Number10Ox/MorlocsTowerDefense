using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EconomySystemTests
{
    private const int STARTING_COINS = 20;
    private const int TURRET_COST = 5;
    private const int COIN_REWARD = 1;

    private static readonly TurretTypeStats DefaultStats = new TurretTypeStats(
        TurretType.Regular, 10f, 1f, 1, 15f, TURRET_COST);

    private static readonly Dictionary<TurretType, TurretTypeStats> DefaultStatsByType =
        new Dictionary<TurretType, TurretTypeStats>
        {
            { TurretType.Regular, DefaultStats }
        };

    private EconomyStore economyStore;
    private TurretStore turretStore;
    private EconomySystem economySystem;

    [SetUp]
    public void SetUp()
    {
        economyStore = new EconomyStore(STARTING_COINS);
        turretStore = new TurretStore();
        economySystem = new EconomySystem(economyStore, turretStore, DefaultStatsByType);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullEconomyStore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EconomySystem(null, turretStore, DefaultStatsByType));
    }

    [Test]
    public void Constructor_NullTurretStore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EconomySystem(economyStore, null, DefaultStatsByType));
    }

    // --- Creep Kill Credits ---

    [Test]
    public void HandleCreepKilled_BuffersCredit_AppliedOnTick()
    {
        economySystem.HandleCreepKilled(0, COIN_REWARD);
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + COIN_REWARD, economyStore.CurrentCoins);
    }

    [Test]
    public void HandleCreepKilled_MultipleKills_AccumulatesCredits()
    {
        economySystem.HandleCreepKilled(0, COIN_REWARD);
        economySystem.HandleCreepKilled(1, COIN_REWARD);
        economySystem.HandleCreepKilled(2, COIN_REWARD);
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + 3 * COIN_REWARD, economyStore.CurrentCoins);
    }

    [Test]
    public void HandleCreepKilled_DifferentRewards_SumsCorrectly()
    {
        economySystem.HandleCreepKilled(0, 1);
        economySystem.HandleCreepKilled(1, 3);
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + 4, economyStore.CurrentCoins);
    }

    [Test]
    public void HandleCreepKilled_ZeroReward_NoCredit()
    {
        economySystem.HandleCreepKilled(0, 0);
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS, economyStore.CurrentCoins);
    }

    [Test]
    public void HandleCreepKilled_CreditsNotAppliedUntilTick()
    {
        economySystem.HandleCreepKilled(0, COIN_REWARD);

        Assert.AreEqual(STARTING_COINS, economyStore.CurrentCoins);
    }

    [Test]
    public void Tick_ClearsPendingCreditsAfterApply()
    {
        economySystem.HandleCreepKilled(0, COIN_REWARD);
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + COIN_REWARD, economyStore.CurrentCoins);

        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + COIN_REWARD, economyStore.CurrentCoins);
    }

    // --- Turret Placement Debits ---

    [Test]
    public void Tick_TurretPlacedThisFrame_DeductsCoins()
    {
        turretStore.Add(MakeTurret(0));
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS - TURRET_COST, economyStore.CurrentCoins);
    }

    [Test]
    public void Tick_MultipleTurretsPlaced_DeductsForEach()
    {
        turretStore.Add(MakeTurret(0));
        turretStore.Add(MakeTurret(1));
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS - 2 * TURRET_COST, economyStore.CurrentCoins);
    }

    [Test]
    public void Tick_NoTurretsPlaced_NoDeduction()
    {
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS, economyStore.CurrentCoins);
    }

    // --- Combined Credits + Debits ---

    [Test]
    public void Tick_KillAndPlaceSameFrame_CreditsAppliedBeforeDebits()
    {
        economySystem.HandleCreepKilled(0, COIN_REWARD);
        turretStore.Add(MakeTurret(0));
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + COIN_REWARD - TURRET_COST, economyStore.CurrentCoins);
    }

    [Test]
    public void Tick_CreditsEnablePlacement_TrySpendSucceeds()
    {
        // Tests EconomySystem's internal ordering: credits applied before debits within Tick().
        // In real gameplay, PlacementSystem gates on CanAfford() in Phase 1 using the prior
        // frame's balance, so kill rewards from Phase 3 cannot fund same-frame placement.
        // Start with exactly the turret cost, kill adds 1 more
        var tightStore = new EconomyStore(TURRET_COST);
        var system = new EconomySystem(tightStore, turretStore, DefaultStatsByType);

        system.HandleCreepKilled(0, COIN_REWARD);
        turretStore.Add(MakeTurret(0));
        system.Tick(0.016f);

        Assert.AreEqual(COIN_REWARD, tightStore.CurrentCoins);
    }

    // --- Reset ---

    [Test]
    public void Reset_ClearsPendingCredits()
    {
        economySystem.HandleCreepKilled(0, COIN_REWARD);
        economySystem.Reset();
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS, economyStore.CurrentCoins);
    }

    // --- Helpers ---

    private TurretSimData MakeTurret(int id)
    {
        return new TurretSimData(id)
        {
            Position = Vector3.zero,
            Range = 10f,
            FireInterval = 1f,
            Damage = 1,
            ProjectileSpeed = 15f
        };
    }
}
