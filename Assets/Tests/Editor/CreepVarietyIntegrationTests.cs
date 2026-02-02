using NUnit.Framework;
using UnityEngine;

public class CreepVarietyIntegrationTests
{
    private const float SMALL_SPEED = 10f;
    private const float BIG_SPEED = 3f;
    private const int SMALL_MAX_HEALTH = 2;
    private const int BIG_MAX_HEALTH = 8;
    private const int SMALL_DAMAGE_TO_BASE = 1;
    private const int BIG_DAMAGE_TO_BASE = 3;
    private const int SMALL_COIN_REWARD = 1;
    private const int BIG_COIN_REWARD = 3;
    private const float SPAWN_INTERVAL = 1f;
    private const float ARRIVAL_THRESHOLD = 0.5f;

    private static readonly CreepTypeStats SmallStats = new CreepTypeStats(
        CreepType.Small, SMALL_SPEED, SMALL_DAMAGE_TO_BASE, SMALL_MAX_HEALTH, SMALL_COIN_REWARD);

    private static readonly CreepTypeStats BigStats = new CreepTypeStats(
        CreepType.Big, BIG_SPEED, BIG_DAMAGE_TO_BASE, BIG_MAX_HEALTH, BIG_COIN_REWARD);

    private static readonly CreepTypeStats[] TwoTypes = new CreepTypeStats[] { SmallStats, BigStats };

    private CreepStore creepStore;
    private Vector3[] singleSpawnPoint;
    private Vector3 basePosition;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        singleSpawnPoint = new[] { new Vector3(10f, 0f, 0f) };
        basePosition = Vector3.zero;
    }

    private SpawnSystem MakeSpawnSystem(
        CreepTypeStats[] orderedStats = null,
        int perSpawn = 1)
    {
        return new SpawnSystem(
            creepStore,
            singleSpawnPoint,
            basePosition,
            SPAWN_INTERVAL,
            perSpawn,
            orderedStats ?? TwoTypes);
    }

    // --- Round-Robin Cycling ---

    [Test]
    public void RoundRobin_FirstCreep_IsFirstType()
    {
        var system = MakeSpawnSystem(perSpawn: 1);

        system.Tick(SPAWN_INTERVAL);

        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
    }

    [Test]
    public void RoundRobin_SecondCreep_IsSecondType()
    {
        var system = MakeSpawnSystem(perSpawn: 2);

        system.Tick(SPAWN_INTERVAL);

        Assert.AreEqual(2, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
        Assert.AreEqual(CreepType.Big, creepStore.ActiveCreeps[1].Type);
    }

    [Test]
    public void RoundRobin_CyclesAcrossBursts()
    {
        // perSpawn=1, singleSpawnPoint → 1 creep per burst
        // Burst 1: Small, Burst 2: Big, Burst 3: Small
        var system = MakeSpawnSystem(perSpawn: 1);

        system.Tick(SPAWN_INTERVAL);
        creepStore.BeginFrame();
        system.Tick(SPAWN_INTERVAL);
        creepStore.BeginFrame();
        system.Tick(SPAWN_INTERVAL);

        Assert.AreEqual(3, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
        Assert.AreEqual(CreepType.Big, creepStore.ActiveCreeps[1].Type);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[2].Type);
    }

    [Test]
    public void Reset_ResetsTypeIndex_RestartsFromFirstType()
    {
        var system = MakeSpawnSystem(perSpawn: 1);

        // Spawn one creep (Small), advance index to Big
        system.Tick(SPAWN_INTERVAL);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);

        // Reset and spawn again — should restart from Small
        system.Reset();
        creepStore.BeginFrame();
        system.Tick(SPAWN_INTERVAL);

        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
    }

    [Test]
    public void SpawnedCreeps_HaveCorrectTypeField()
    {
        var system = MakeSpawnSystem(perSpawn: 2);

        system.Tick(SPAWN_INTERVAL);

        var small = creepStore.ActiveCreeps[0];
        var big = creepStore.ActiveCreeps[1];

        Assert.AreEqual(CreepType.Small, small.Type);
        Assert.AreEqual(SMALL_SPEED, small.Speed, 0.001f);
        Assert.AreEqual(SMALL_MAX_HEALTH, small.Health);
        Assert.AreEqual(SMALL_MAX_HEALTH, small.MaxHealth);
        Assert.AreEqual(SMALL_DAMAGE_TO_BASE, small.DamageToBase);
        Assert.AreEqual(SMALL_COIN_REWARD, small.CoinReward);

        Assert.AreEqual(CreepType.Big, big.Type);
        Assert.AreEqual(BIG_SPEED, big.Speed, 0.001f);
        Assert.AreEqual(BIG_MAX_HEALTH, big.Health);
        Assert.AreEqual(BIG_MAX_HEALTH, big.MaxHealth);
        Assert.AreEqual(BIG_DAMAGE_TO_BASE, big.DamageToBase);
        Assert.AreEqual(BIG_COIN_REWARD, big.CoinReward);
    }

    // --- Speed Difference (MovementSystem only) ---

    [Test]
    public void SmallAndBig_DifferentSpeeds_SmallArrivesFirst()
    {
        // Create two creeps at the same starting position with different speeds
        var smallCreep = new CreepSimData(0)
        {
            Type = CreepType.Small,
            Position = new Vector3(10f, 0f, 0f),
            Target = Vector3.zero,
            Speed = SMALL_SPEED,
            Health = SMALL_MAX_HEALTH,
            MaxHealth = SMALL_MAX_HEALTH,
            DamageToBase = SMALL_DAMAGE_TO_BASE
        };
        var bigCreep = new CreepSimData(1)
        {
            Type = CreepType.Big,
            Position = new Vector3(10f, 0f, 0f),
            Target = Vector3.zero,
            Speed = BIG_SPEED,
            Health = BIG_MAX_HEALTH,
            MaxHealth = BIG_MAX_HEALTH,
            DamageToBase = BIG_DAMAGE_TO_BASE
        };

        creepStore.Add(smallCreep);
        creepStore.Add(bigCreep);

        var movementSystem = new MovementSystem(creepStore, arrivalThreshold: ARRIVAL_THRESHOLD);

        // Tick until small arrives
        for (int i = 0; i < 100; i++)
        {
            if (smallCreep.ReachedBase) break;
            movementSystem.Tick(0.1f);
        }

        Assert.IsTrue(smallCreep.ReachedBase, "Small creep should have reached base");
        Assert.IsFalse(bigCreep.ReachedBase, "Big creep should NOT have reached base yet");
    }

    // --- Health Difference (DamageSystem + RecordHit only) ---

    [Test]
    public void SmallAndBig_DifferentHealth_BigSurvivesMoreHits()
    {
        var projectileStore = new ProjectileStore();
        var baseStore = new BaseStore(100);
        var damageSystem = new DamageSystem(creepStore, baseStore, projectileStore);

        var smallCreep = new CreepSimData(0)
        {
            Type = CreepType.Small,
            Position = new Vector3(5f, 0f, 0f),
            Target = Vector3.zero,
            Speed = SMALL_SPEED,
            Health = SMALL_MAX_HEALTH,
            MaxHealth = SMALL_MAX_HEALTH,
            DamageToBase = SMALL_DAMAGE_TO_BASE
        };
        var bigCreep = new CreepSimData(1)
        {
            Type = CreepType.Big,
            Position = new Vector3(5f, 0f, 0f),
            Target = Vector3.zero,
            Speed = BIG_SPEED,
            Health = BIG_MAX_HEALTH,
            MaxHealth = BIG_MAX_HEALTH,
            DamageToBase = BIG_DAMAGE_TO_BASE
        };

        creepStore.Add(smallCreep);
        creepStore.Add(bigCreep);

        // Hit both creeps with 1 damage per hit, enough to kill small but not big
        int hitDamage = 1;
        for (int i = 0; i < SMALL_MAX_HEALTH; i++)
        {
            projectileStore.RecordHit(new ProjectileHit(0, hitDamage));
            projectileStore.RecordHit(new ProjectileHit(1, hitDamage));
            damageSystem.Tick(0.016f);
            projectileStore.BeginFrame();
        }

        Assert.AreEqual(0, smallCreep.Health, "Small creep should be dead");
        Assert.Greater(bigCreep.Health, 0, "Big creep should still be alive");
        Assert.AreEqual(BIG_MAX_HEALTH - SMALL_MAX_HEALTH, bigCreep.Health);
    }
}
