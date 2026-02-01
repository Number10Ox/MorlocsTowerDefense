using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TurretTypesIntegrationTests
{
    private const int BASE_MAX_HEALTH = 100;
    private const int HIGH_STARTING_COINS = 1000;
    private const float TURRET_RANGE = 20f;
    private const float TURRET_FIRE_INTERVAL = 1f;
    private const int TURRET_DAMAGE = 1;
    private const float PROJECTILE_SPEED = 100f;
    private const int REGULAR_COST = 5;
    private const int FREEZING_COST = 8;
    private const float SLOW_DURATION = 2f;
    private const float SLOW_MULTIPLIER = 0.5f;
    private const float CREEP_SPEED = 10f;

    private static readonly TurretTypeStats RegularStats = new TurretTypeStats(
        TurretType.Regular, TURRET_RANGE, TURRET_FIRE_INTERVAL, TURRET_DAMAGE,
        PROJECTILE_SPEED, REGULAR_COST);

    private static readonly TurretTypeStats FreezingStats = new TurretTypeStats(
        TurretType.Freezing, TURRET_RANGE, TURRET_FIRE_INTERVAL, TURRET_DAMAGE,
        PROJECTILE_SPEED, FREEZING_COST,
        SLOW_DURATION, SLOW_MULTIPLIER);

    private static readonly Dictionary<TurretType, TurretTypeStats> StatsByType =
        new Dictionary<TurretType, TurretTypeStats>
        {
            { TurretType.Regular, RegularStats },
            { TurretType.Freezing, FreezingStats }
        };

    private CreepStore creepStore;
    private BaseStore baseStore;
    private TurretStore turretStore;
    private ProjectileStore projectileStore;
    private EconomyStore economyStore;
    private PlacementInput placementInput;
    private TurretSelectionStore selectionStore;

    private MovementSystem movementSystem;
    private PlacementSystem placementSystem;
    private ProjectileSystem projectileSystem;
    private DamageSystem damageSystem;
    private EconomySystem economySystem;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        baseStore = new BaseStore(BASE_MAX_HEALTH);
        turretStore = new TurretStore();
        projectileStore = new ProjectileStore();
        economyStore = new EconomyStore(HIGH_STARTING_COINS);
        placementInput = new PlacementInput();
        selectionStore = new TurretSelectionStore(TurretType.Regular);

        movementSystem = new MovementSystem(creepStore);
        placementSystem = new PlacementSystem(
            turretStore, placementInput, economyStore,
            selectionStore, StatsByType);
        projectileSystem = new ProjectileSystem(turretStore, creepStore, projectileStore);
        damageSystem = new DamageSystem(creepStore, baseStore, projectileStore);
        economySystem = new EconomySystem(economyStore, turretStore, StatsByType);

        damageSystem.OnCreepKilled += economySystem.HandleCreepKilled;
    }

    private void BeginFrame()
    {
        creepStore.BeginFrame();
        baseStore.BeginFrame();
        turretStore.BeginFrame();
        projectileStore.BeginFrame();
        economyStore.BeginFrame();
    }

    private void TickPhase1(float dt)
    {
        movementSystem.Tick(dt);
        placementSystem.Tick(dt);
    }

    private void TickPhase2(float dt)
    {
        projectileSystem.Tick(dt);
        damageSystem.Tick(dt);
    }

    private void TickPhase3(float dt)
    {
        economySystem.Tick(dt);
    }

    private void TickAllPhases(float dt)
    {
        TickPhase1(dt);
        TickPhase2(dt);
        TickPhase3(dt);
    }

    private CreepSimData AddCreep(int id, Vector3 position, int health)
    {
        var creep = new CreepSimData(id)
        {
            Position = position,
            Target = Vector3.zero,
            Speed = CREEP_SPEED,
            Health = health,
            MaxHealth = health,
            CoinReward = 1,
            DamageToBase = 1
        };
        creepStore.Add(creep);
        return creep;
    }

    // =============================================
    // Freezing Turret Full Cycle
    // =============================================

    [Test]
    public void FreezingTurret_FullCycle()
    {
        // Place freezing turret via PlacementSystem
        selectionStore.SelectType(TurretType.Freezing);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        placementSystem.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(TurretType.Freezing, turretStore.ActiveTurrets[0].Type);

        // Spawn creep in range, close enough for instant projectile hit.
        // Target set far away so creep doesn't reach base during movement assertions.
        var creep = AddCreep(0, new Vector3(0.3f, 0f, 0f), health: 10);
        creep.Target = new Vector3(100f, 0f, 0f);

        // Combat tick: turret fires, projectile hits, slow applied
        TickPhase2(0.016f);

        Assert.AreEqual(9, creep.Health);
        Assert.AreEqual(SLOW_DURATION, creep.SlowRemainingTime, 0.01f);
        Assert.AreEqual(SLOW_MULTIPLIER, creep.SlowMultiplier);

        // Movement tick: creep should move at reduced speed
        BeginFrame();
        Vector3 posBeforeMove = creep.Position;
        float dt = 1f;
        movementSystem.Tick(dt);

        float distanceMoved = (creep.Position - posBeforeMove).magnitude;
        float expectedSlowDistance = CREEP_SPEED * SLOW_MULTIPLIER * dt;
        Assert.AreEqual(expectedSlowDistance, distanceMoved, 0.01f);

        // Tick DamageSystem to decrement slow timer
        damageSystem.Tick(dt);

        // Tick until slow expires (tolerance-based — effective duration is [SLOW_DURATION, SLOW_DURATION + dt])
        float remainingSlowTime = creep.SlowRemainingTime;
        while (remainingSlowTime > 0f)
        {
            BeginFrame();
            damageSystem.Tick(0.5f);
            remainingSlowTime = creep.SlowRemainingTime;
        }

        Assert.AreEqual(0f, creep.SlowRemainingTime);

        // Creep should now move at full speed
        BeginFrame();
        Vector3 posBeforeFullSpeed = creep.Position;
        movementSystem.Tick(dt);

        float fullSpeedDistance = (creep.Position - posBeforeFullSpeed).magnitude;
        float expectedFullDistance = CREEP_SPEED * dt;
        Assert.AreEqual(expectedFullDistance, fullSpeedDistance, 0.01f);
    }

    // =============================================
    // Regular and Freezing Coexistence
    // =============================================

    [Test]
    public void RegularAndFreezingTurrets_Coexist()
    {
        // Place a regular turret
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = new Vector3(-5f, 0f, 0f);
        placementSystem.Tick(0.016f);

        BeginFrame();

        // Place a freezing turret
        selectionStore.SelectType(TurretType.Freezing);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = new Vector3(5f, 0f, 0f);
        placementSystem.Tick(0.016f);

        Assert.AreEqual(2, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(TurretType.Regular, turretStore.ActiveTurrets[0].Type);
        Assert.AreEqual(TurretType.Freezing, turretStore.ActiveTurrets[1].Type);

        // Spawn two creeps, one near each turret
        var creepNearRegular = AddCreep(0, new Vector3(-4.7f, 0f, 0f), health: 10);
        var creepNearFreezing = AddCreep(1, new Vector3(5.3f, 0f, 0f), health: 10);

        // Combat: both turrets fire
        BeginFrame();
        TickPhase2(0.016f);

        // Regular turret's creep: damaged, no slow
        Assert.AreEqual(9, creepNearRegular.Health);
        Assert.AreEqual(0f, creepNearRegular.SlowRemainingTime);

        // Freezing turret's creep: damaged and slowed
        Assert.AreEqual(9, creepNearFreezing.Health);
        Assert.AreEqual(SLOW_DURATION, creepNearFreezing.SlowRemainingTime, 0.01f);
        Assert.AreEqual(SLOW_MULTIPLIER, creepNearFreezing.SlowMultiplier);
    }

    // =============================================
    // Per-Type Cost Enforcement
    // =============================================

    [Test]
    public void FreezingTurret_CostsMoreThanRegular()
    {
        // Give exactly enough coins for regular but not for freezing
        var tightEconomyStore = new EconomyStore(REGULAR_COST);
        var tightSelectionStore = new TurretSelectionStore(TurretType.Regular);
        var tightPlacement = new PlacementSystem(
            turretStore, placementInput, tightEconomyStore,
            tightSelectionStore, StatsByType);

        // Attempt freezing placement — should fail
        tightSelectionStore.SelectType(TurretType.Freezing);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        tightPlacement.Tick(0.016f);

        Assert.AreEqual(0, turretStore.ActiveTurrets.Count);

        // Attempt regular placement — should succeed
        tightSelectionStore.SelectType(TurretType.Regular);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.one;
        tightPlacement.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
        Assert.AreEqual(TurretType.Regular, turretStore.ActiveTurrets[0].Type);
    }

    // =============================================
    // Economy Integration with Turret Types
    // =============================================

    [Test]
    public void PlaceBothTypes_CorrectCostsDeducted()
    {
        // Place regular turret
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.zero;
        TickAllPhases(0.016f);

        Assert.AreEqual(HIGH_STARTING_COINS - REGULAR_COST, economyStore.CurrentCoins);

        BeginFrame();

        // Place freezing turret
        selectionStore.SelectType(TurretType.Freezing);
        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = Vector3.one;
        TickAllPhases(0.016f);

        Assert.AreEqual(HIGH_STARTING_COINS - REGULAR_COST - FREEZING_COST, economyStore.CurrentCoins);
    }
}
