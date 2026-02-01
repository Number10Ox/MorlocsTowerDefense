using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EconomyIntegrationTests
{
    private const int STARTING_COINS = 10;
    private const int TURRET_COST = 5;
    private const int COIN_REWARD = 1;
    private const int CREEP_MAX_HEALTH = 1;
    private const int TURRET_DAMAGE = 1;

    private static readonly TurretTypeStats DefaultStats = new TurretTypeStats(
        TurretType.Regular, 10f, 1f, TURRET_DAMAGE, 15f, TURRET_COST);

    private static readonly Dictionary<TurretType, TurretTypeStats> DefaultStatsByType =
        new Dictionary<TurretType, TurretTypeStats>
        {
            { TurretType.Regular, DefaultStats }
        };

    private CreepStore creepStore;
    private BaseStore baseStore;
    private TurretStore turretStore;
    private ProjectileStore projectileStore;
    private EconomyStore economyStore;
    private PlacementInput placementInput;
    private TurretSelectionStore selectionStore;

    private DamageSystem damageSystem;
    private PlacementSystem placementSystem;
    private EconomySystem economySystem;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        baseStore = new BaseStore(100);
        turretStore = new TurretStore();
        projectileStore = new ProjectileStore();
        economyStore = new EconomyStore(STARTING_COINS);
        placementInput = new PlacementInput();
        selectionStore = new TurretSelectionStore(TurretType.Regular);

        damageSystem = new DamageSystem(creepStore, baseStore, projectileStore);
        placementSystem = new PlacementSystem(
            turretStore, placementInput, economyStore,
            selectionStore, DefaultStatsByType);
        economySystem = new EconomySystem(economyStore, turretStore, DefaultStatsByType);

        damageSystem.OnCreepKilled += economySystem.HandleCreepKilled;
    }

    [Test]
    public void CreepKill_AwardsCoins()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.forward,
            Speed = 1f,
            Health = CREEP_MAX_HEALTH,
            MaxHealth = CREEP_MAX_HEALTH,
            CoinReward = COIN_REWARD
        };
        creepStore.Add(creep);

        projectileStore.RecordHit(new ProjectileHit(0, TURRET_DAMAGE));

        // Phase 2: DamageSystem kills creep → fires OnCreepKilled → EconomySystem buffers
        damageSystem.Tick(0.016f);
        // Phase 3: EconomySystem applies buffered credits
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + COIN_REWARD, economyStore.CurrentCoins);
    }

    [Test]
    public void PlacementBlocked_InsufficientCoins()
    {
        var brokeStore = new EconomyStore(TURRET_COST - 1);
        var brokeSelStore = new TurretSelectionStore(TurretType.Regular);
        var system = new PlacementSystem(
            turretStore, placementInput, brokeStore,
            brokeSelStore, DefaultStatsByType);

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;

        system.Tick(0.016f);

        Assert.AreEqual(0, turretStore.ActiveTurrets.Count);
        Assert.IsFalse(placementInput.PlaceRequested);
    }

    [Test]
    public void PlacementSucceeds_ExactCoins()
    {
        var exactStore = new EconomyStore(TURRET_COST);
        var exactSelStore = new TurretSelectionStore(TurretType.Regular);
        var system = new PlacementSystem(
            turretStore, placementInput, exactStore,
            exactSelStore, DefaultStatsByType);

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;

        system.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void FullCycle_KillCreepThenPlaceTurret()
    {
        // Add a creep and kill it
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.forward,
            Speed = 1f,
            Health = CREEP_MAX_HEALTH,
            MaxHealth = CREEP_MAX_HEALTH,
            CoinReward = COIN_REWARD
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, TURRET_DAMAGE));

        // Frame 1: kill creep, economy resolves
        damageSystem.Tick(0.016f);
        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS + COIN_REWARD, economyStore.CurrentCoins);

        // Frame 2: flush stores, request placement
        creepStore.BeginFrame();
        turretStore.BeginFrame();
        projectileStore.BeginFrame();
        economyStore.BeginFrame();

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;

        placementSystem.Tick(0.016f);
        economySystem.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(STARTING_COINS + COIN_REWARD - TURRET_COST, economyStore.CurrentCoins);
    }
}
