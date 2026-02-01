using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TurretTypesTests
{
    private const float DEFAULT_RANGE = 10f;
    private const float DEFAULT_FIRE_INTERVAL = 1f;
    private const int DEFAULT_DAMAGE = 1;
    private const float DEFAULT_PROJECTILE_SPEED = 15f;
    private const int REGULAR_COST = 5;
    private const int FREEZING_COST = 8;
    private const float SLOW_DURATION = 2f;
    private const float SLOW_MULTIPLIER = 0.5f;
    private const int BASE_MAX_HEALTH = 100;
    private const int HIGH_STARTING_COINS = 1000;

    private static readonly TurretTypeStats RegularStats = new TurretTypeStats(
        TurretType.Regular, DEFAULT_RANGE, DEFAULT_FIRE_INTERVAL, DEFAULT_DAMAGE,
        DEFAULT_PROJECTILE_SPEED, REGULAR_COST);

    private static readonly TurretTypeStats FreezingStats = new TurretTypeStats(
        TurretType.Freezing, DEFAULT_RANGE, DEFAULT_FIRE_INTERVAL, DEFAULT_DAMAGE,
        DEFAULT_PROJECTILE_SPEED, FREEZING_COST,
        SLOW_DURATION, SLOW_MULTIPLIER);

    private static readonly Dictionary<TurretType, TurretTypeStats> StatsByType =
        new Dictionary<TurretType, TurretTypeStats>
        {
            { TurretType.Regular, RegularStats },
            { TurretType.Freezing, FreezingStats }
        };

    // =============================================
    // TurretSelectionStore
    // =============================================

    [Test]
    public void SelectType_ChangesSelectedType()
    {
        var store = new TurretSelectionStore(TurretType.Regular);

        store.SelectType(TurretType.Freezing);

        Assert.AreEqual(TurretType.Freezing, store.SelectedType);
    }

    [Test]
    public void SelectType_FiresEvent_WhenTypeChanges()
    {
        var store = new TurretSelectionStore(TurretType.Regular);
        TurretType? received = null;
        store.OnSelectionChanged += type => received = type;

        store.SelectType(TurretType.Freezing);

        Assert.AreEqual(TurretType.Freezing, received);
    }

    [Test]
    public void SelectType_DoesNotFireEvent_WhenSameType()
    {
        var store = new TurretSelectionStore(TurretType.Regular);
        int fireCount = 0;
        store.OnSelectionChanged += _ => fireCount++;

        store.SelectType(TurretType.Regular);

        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void Reset_RestoresDefault()
    {
        var store = new TurretSelectionStore(TurretType.Regular);
        store.SelectType(TurretType.Freezing);

        store.Reset();

        Assert.AreEqual(TurretType.Regular, store.SelectedType);
    }

    // =============================================
    // PlacementSystem — Turret Type Selection
    // =============================================

    [Test]
    public void PlacesRegularTurret_WithCorrectStats()
    {
        var turretStore = new TurretStore();
        var input = new PlacementInput();
        var economy = new EconomyStore(HIGH_STARTING_COINS);
        var selection = new TurretSelectionStore(TurretType.Regular);
        var system = new PlacementSystem(turretStore, input, economy, selection, StatsByType);

        input.PlaceRequested = true;
        input.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        var turret = turretStore.ActiveTurrets[0];
        Assert.AreEqual(TurretType.Regular, turret.Type);
        Assert.AreEqual(0f, turret.SlowDuration);
        Assert.AreEqual(1f, turret.SlowMultiplier);
    }

    [Test]
    public void PlacesFreezingTurret_WithCorrectStats()
    {
        var turretStore = new TurretStore();
        var input = new PlacementInput();
        var economy = new EconomyStore(HIGH_STARTING_COINS);
        var selection = new TurretSelectionStore(TurretType.Regular);
        selection.SelectType(TurretType.Freezing);
        var system = new PlacementSystem(turretStore, input, economy, selection, StatsByType);

        input.PlaceRequested = true;
        input.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        var turret = turretStore.ActiveTurrets[0];
        Assert.AreEqual(TurretType.Freezing, turret.Type);
        Assert.AreEqual(SLOW_DURATION, turret.SlowDuration);
        Assert.AreEqual(SLOW_MULTIPLIER, turret.SlowMultiplier);
    }

    [Test]
    public void PlacesFreezingTurret_ChecksFreezingCost()
    {
        var turretStore = new TurretStore();
        var input = new PlacementInput();
        // Enough for regular but not freezing
        var economy = new EconomyStore(REGULAR_COST);
        var selection = new TurretSelectionStore(TurretType.Regular);
        selection.SelectType(TurretType.Freezing);
        var system = new PlacementSystem(turretStore, input, economy, selection, StatsByType);

        input.PlaceRequested = true;
        input.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        Assert.AreEqual(0, turretStore.ActiveTurrets.Count);
    }

    [Test]
    public void PlacesRegularTurret_ChecksRegularCost()
    {
        var turretStore = new TurretStore();
        var input = new PlacementInput();
        var economy = new EconomyStore(REGULAR_COST);
        var selection = new TurretSelectionStore(TurretType.Regular);
        var system = new PlacementSystem(turretStore, input, economy, selection, StatsByType);

        input.PlaceRequested = true;
        input.WorldPosition = Vector3.zero;
        system.Tick(0.016f);

        Assert.AreEqual(1, turretStore.ActiveTurrets.Count);
    }

    // =============================================
    // DamageSystem — Slow Effect Application
    // =============================================

    [Test]
    public void FreezingProjectileHit_AppliesSlowEffect()
    {
        var creepStore = new CreepStore();
        var baseStore = new BaseStore(BASE_MAX_HEALTH);
        var projectileStore = new ProjectileStore();
        var system = new DamageSystem(creepStore, baseStore, projectileStore);

        var creep = MakeCreep(0, health: 10);
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 1, SLOW_DURATION, SLOW_MULTIPLIER));

        system.Tick(0.016f);

        Assert.AreEqual(SLOW_DURATION, creep.SlowRemainingTime);
        Assert.AreEqual(SLOW_MULTIPLIER, creep.SlowMultiplier);
    }

    [Test]
    public void RegularProjectileHit_DoesNotApplySlowEffect()
    {
        var creepStore = new CreepStore();
        var baseStore = new BaseStore(BASE_MAX_HEALTH);
        var projectileStore = new ProjectileStore();
        var system = new DamageSystem(creepStore, baseStore, projectileStore);

        var creep = MakeCreep(0, health: 10);
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 1));

        system.Tick(0.016f);

        Assert.AreEqual(0f, creep.SlowRemainingTime);
    }

    [Test]
    public void FreezingHit_RefreshesExistingSlowDuration()
    {
        var creepStore = new CreepStore();
        var baseStore = new BaseStore(BASE_MAX_HEALTH);
        var projectileStore = new ProjectileStore();
        var system = new DamageSystem(creepStore, baseStore, projectileStore);

        var creep = MakeCreep(0, health: 10);
        creep.SlowRemainingTime = 0.5f;
        creep.SlowMultiplier = 0.8f;
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 1, SLOW_DURATION, SLOW_MULTIPLIER));

        system.Tick(0.016f);

        Assert.AreEqual(SLOW_DURATION, creep.SlowRemainingTime);
        Assert.AreEqual(SLOW_MULTIPLIER, creep.SlowMultiplier);
    }

    [Test]
    public void SlowRemainingTime_DecrementsEachTick()
    {
        var creepStore = new CreepStore();
        var baseStore = new BaseStore(BASE_MAX_HEALTH);
        var projectileStore = new ProjectileStore();
        var system = new DamageSystem(creepStore, baseStore, projectileStore);

        var creep = MakeCreep(0, health: 10);
        creep.SlowRemainingTime = 2f;
        creep.SlowMultiplier = 0.5f;
        creepStore.Add(creep);

        float dt = 0.5f;
        system.Tick(dt);

        Assert.AreEqual(1.5f, creep.SlowRemainingTime, 0.001f);
    }

    [Test]
    public void SlowEffect_Expires_WhenTimerReachesZero()
    {
        var creepStore = new CreepStore();
        var baseStore = new BaseStore(BASE_MAX_HEALTH);
        var projectileStore = new ProjectileStore();
        var system = new DamageSystem(creepStore, baseStore, projectileStore);

        var creep = MakeCreep(0, health: 10);
        creep.SlowRemainingTime = 0.1f;
        creep.SlowMultiplier = 0.5f;
        creepStore.Add(creep);

        system.Tick(0.2f);

        Assert.AreEqual(0f, creep.SlowRemainingTime);
    }

    [Test]
    public void FreezingHit_StillAppliesDamage()
    {
        var creepStore = new CreepStore();
        var baseStore = new BaseStore(BASE_MAX_HEALTH);
        var projectileStore = new ProjectileStore();
        var system = new DamageSystem(creepStore, baseStore, projectileStore);

        var creep = MakeCreep(0, health: 5);
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 2, SLOW_DURATION, SLOW_MULTIPLIER));

        system.Tick(0.016f);

        Assert.AreEqual(3, creep.Health);
        Assert.AreEqual(SLOW_DURATION, creep.SlowRemainingTime);
    }

    // =============================================
    // MovementSystem — Slow Speed Reduction
    // =============================================

    [Test]
    public void SlowedCreep_MovesAtReducedSpeed()
    {
        var creepStore = new CreepStore();
        float speed = 10f;
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = new Vector3(100f, 0f, 0f),
            Speed = speed,
            Health = 1,
            MaxHealth = 1,
            SlowRemainingTime = 5f,
            SlowMultiplier = SLOW_MULTIPLIER
        };
        creepStore.Add(creep);
        var system = new MovementSystem(creepStore);

        float dt = 1f;
        system.Tick(dt);

        float expectedDistance = speed * SLOW_MULTIPLIER * dt;
        Assert.AreEqual(expectedDistance, creep.Position.x, 0.001f);
    }

    [Test]
    public void UnslowedCreep_MovesAtFullSpeed()
    {
        var creepStore = new CreepStore();
        float speed = 10f;
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = new Vector3(100f, 0f, 0f),
            Speed = speed,
            Health = 1,
            MaxHealth = 1,
            SlowRemainingTime = 0f,
            SlowMultiplier = 0f
        };
        creepStore.Add(creep);
        var system = new MovementSystem(creepStore);

        float dt = 1f;
        system.Tick(dt);

        float expectedDistance = speed * dt;
        Assert.AreEqual(expectedDistance, creep.Position.x, 0.001f);
    }

    [Test]
    public void SlowExpired_CreepMovesAtFullSpeed()
    {
        var creepStore = new CreepStore();
        float speed = 10f;
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = new Vector3(100f, 0f, 0f),
            Speed = speed,
            Health = 1,
            MaxHealth = 1,
            SlowRemainingTime = 0f,
            SlowMultiplier = 0.5f
        };
        creepStore.Add(creep);
        var system = new MovementSystem(creepStore);

        float dt = 1f;
        system.Tick(dt);

        // SlowRemainingTime is 0, so multiplier should not apply
        float expectedDistance = speed * dt;
        Assert.AreEqual(expectedDistance, creep.Position.x, 0.001f);
    }

    // =============================================
    // EconomySystem — Per-Type Costs
    // =============================================

    [Test]
    public void RegularTurret_DeductsRegularCost()
    {
        var economyStore = new EconomyStore(HIGH_STARTING_COINS);
        var turretStore = new TurretStore();
        var system = new EconomySystem(economyStore, turretStore, StatsByType);

        var turret = new TurretSimData(0) { Type = TurretType.Regular };
        turretStore.Add(turret);
        system.Tick(0.016f);

        Assert.AreEqual(HIGH_STARTING_COINS - REGULAR_COST, economyStore.CurrentCoins);
    }

    [Test]
    public void FreezingTurret_DeductsFreezingCost()
    {
        var economyStore = new EconomyStore(HIGH_STARTING_COINS);
        var turretStore = new TurretStore();
        var system = new EconomySystem(economyStore, turretStore, StatsByType);

        var turret = new TurretSimData(0) { Type = TurretType.Freezing };
        turretStore.Add(turret);
        system.Tick(0.016f);

        Assert.AreEqual(HIGH_STARTING_COINS - FREEZING_COST, economyStore.CurrentCoins);
    }

    // =============================================
    // Helpers
    // =============================================

    private CreepSimData MakeCreep(int id, int health)
    {
        return new CreepSimData(id)
        {
            Position = Vector3.zero,
            Target = new Vector3(100f, 0f, 0f),
            Speed = 5f,
            Health = health,
            MaxHealth = health,
            CoinReward = 1,
            DamageToBase = 1
        };
    }
}
