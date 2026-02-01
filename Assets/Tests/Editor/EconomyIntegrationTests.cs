using NUnit.Framework;
using UnityEngine;

public class EconomyIntegrationTests
{
    private const int STARTING_COINS = 10;
    private const int TURRET_COST = 5;
    private const int COIN_REWARD = 1;
    private const int CREEP_MAX_HEALTH = 1;
    private const int TURRET_DAMAGE = 1;

    private CreepStore creepStore;
    private BaseStore baseStore;
    private TurretStore turretStore;
    private ProjectileStore projectileStore;
    private EconomyStore economyStore;
    private PlacementInput placementInput;

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

        damageSystem = new DamageSystem(creepStore, baseStore, projectileStore);
        placementSystem = new PlacementSystem(
            turretStore, placementInput, economyStore,
            10f, 1f, TURRET_DAMAGE, 15f, TURRET_COST);
        economySystem = new EconomySystem(economyStore, turretStore, TURRET_COST);

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
        var system = new PlacementSystem(
            turretStore, placementInput, brokeStore,
            10f, 1f, 1, 15f, TURRET_COST);

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
        var system = new PlacementSystem(
            turretStore, placementInput, exactStore,
            10f, 1f, 1, 15f, TURRET_COST);

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
