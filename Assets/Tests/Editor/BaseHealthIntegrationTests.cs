using NUnit.Framework;
using UnityEngine;

public class BaseHealthIntegrationTests
{
    private const int BASE_MAX_HEALTH = 50;
    private const int DAMAGE_PER_CREEP = 10;
    private const float CREEP_SPEED = 10f;
    private const float SPAWN_INTERVAL = 1f;
    private const float ARRIVAL_THRESHOLD = 0.5f;

    private CreepStore creepStore;
    private BaseStore baseStore;
    private SpawnSystem spawnSystem;
    private MovementSystem movementSystem;
    private DamageSystem damageSystem;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        baseStore = new BaseStore(BASE_MAX_HEALTH);

        var spawnPositions = new[] { new Vector3(5f, 0f, 0f) };
        var basePosition = Vector3.zero;

        spawnSystem = new SpawnSystem(
            creepStore,
            spawnPositions,
            basePosition,
            spawnInterval: SPAWN_INTERVAL,
            creepsPerSpawn: 1,
            creepSpeed: CREEP_SPEED,
            damageToBase: DAMAGE_PER_CREEP,
            maxHealth: 3);

        movementSystem = new MovementSystem(creepStore, arrivalThreshold: ARRIVAL_THRESHOLD);
        damageSystem = new DamageSystem(creepStore, baseStore, new ProjectileStore());
    }

    [Test]
    public void CreepReachesBase_DealsDamage_BaseHealthDecreases()
    {
        // Spawn a creep
        creepStore.BeginFrame();
        baseStore.BeginFrame();
        spawnSystem.Tick(SPAWN_INTERVAL);

        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);

        // Move creep until it arrives (5 units at speed 10 = ~0.5s)
        for (int i = 0; i < 100; i++)
        {
            if (creepStore.ActiveCreeps.Count > 0 && creepStore.ActiveCreeps[0].ReachedBase)
                break;
            movementSystem.Tick(0.1f);
        }

        Assert.IsTrue(creepStore.ActiveCreeps[0].ReachedBase, "Creep should have arrived");

        // DamageSystem ticks after MovementSystem — same frame, correct ordering
        damageSystem.Tick(0.016f);

        Assert.AreEqual(BASE_MAX_HEALTH - DAMAGE_PER_CREEP, baseStore.CurrentHealth);
        Assert.AreEqual(DAMAGE_PER_CREEP, baseStore.DamageTakenThisFrame);
    }

    [Test]
    public void HasDealtBaseDamage_PreventDoubleDamage_OnReTick()
    {
        // Spawn and move to arrival
        creepStore.BeginFrame();
        baseStore.BeginFrame();
        spawnSystem.Tick(SPAWN_INTERVAL);

        for (int i = 0; i < 100; i++)
        {
            if (creepStore.ActiveCreeps[0].ReachedBase) break;
            movementSystem.Tick(0.1f);
        }

        // First damage tick
        damageSystem.Tick(0.016f);
        Assert.AreEqual(BASE_MAX_HEALTH - DAMAGE_PER_CREEP, baseStore.CurrentHealth);

        // Second damage tick — same frame, creep still in ActiveCreeps (deferred removal)
        damageSystem.Tick(0.016f);
        Assert.AreEqual(BASE_MAX_HEALTH - DAMAGE_PER_CREEP, baseStore.CurrentHealth,
            "Damage should not be applied twice due to HasDealtBaseDamage guard");
    }

    [Test]
    public void SchedulerOrdering_DamageOnlyAfterMovementSetsReachedBase()
    {
        // Spawn a creep far enough that it won't arrive immediately
        creepStore.BeginFrame();
        baseStore.BeginFrame();
        spawnSystem.Tick(SPAWN_INTERVAL);

        // Tick damage BEFORE movement — should not deal damage
        damageSystem.Tick(0.016f);
        Assert.AreEqual(BASE_MAX_HEALTH, baseStore.CurrentHealth,
            "DamageSystem should not deal damage before MovementSystem sets ReachedBase");

        // Now tick movement until arrival
        for (int i = 0; i < 100; i++)
        {
            if (creepStore.ActiveCreeps[0].ReachedBase) break;
            movementSystem.Tick(0.1f);
        }

        // Tick damage after movement
        damageSystem.Tick(0.016f);
        Assert.AreEqual(BASE_MAX_HEALTH - DAMAGE_PER_CREEP, baseStore.CurrentHealth);
    }

    [Test]
    public void EnoughCreepsReachBase_BaseDestroyed_PlayingStateFiresBaseDestroyed()
    {
        GameTrigger? firedTrigger = null;
        var playingState = new PlayingState(
            trigger => firedTrigger = trigger,
            baseStore);
        playingState.Enter();

        // Need 5 creeps at 10 damage each to destroy base with 50 HP
        int creepsNeeded = BASE_MAX_HEALTH / DAMAGE_PER_CREEP;

        for (int wave = 0; wave < creepsNeeded; wave++)
        {
            creepStore.BeginFrame();
            baseStore.BeginFrame();
            spawnSystem.Tick(SPAWN_INTERVAL);

            // Move creep to base
            for (int f = 0; f < 100; f++)
            {
                var active = creepStore.ActiveCreeps;
                bool allArrived = true;
                for (int i = 0; i < active.Count; i++)
                {
                    if (!active[i].ReachedBase)
                    {
                        allArrived = false;
                        break;
                    }
                }
                if (allArrived && active.Count > 0) break;
                movementSystem.Tick(0.1f);
            }

            damageSystem.Tick(0.016f);
            playingState.Tick(0.016f);

            if (firedTrigger.HasValue) break;
        }

        Assert.IsTrue(baseStore.IsDestroyed, "Base should be destroyed");
        Assert.AreEqual(GameTrigger.BaseDestroyed, firedTrigger, "PlayingState should fire BaseDestroyed");
    }

    [Test]
    public void FullFlow_SpawnMoveArriveDamage_BaseHealthTracked()
    {
        int healthChangeCount = 0;
        baseStore.OnBaseHealthChanged += (current, max) => healthChangeCount++;

        // Run several game frames with the full system pipeline
        for (int frame = 0; frame < 30; frame++)
        {
            creepStore.BeginFrame();
            baseStore.BeginFrame();
            spawnSystem.Tick(0.5f);
            movementSystem.Tick(0.5f);
            damageSystem.Tick(0.5f);
        }

        // After 30 frames at 0.5s each = 15s of game time,
        // with 1s spawn interval and 5-unit distance at speed 10,
        // multiple creeps should have arrived and dealt damage
        Assert.Less(baseStore.CurrentHealth, BASE_MAX_HEALTH, "Base should have taken some damage");
        Assert.Greater(healthChangeCount, 0, "OnBaseHealthChanged should have fired");
    }
}
