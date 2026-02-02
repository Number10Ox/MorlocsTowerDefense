using System.Collections.Generic;
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
    private const float ARRIVAL_THRESHOLD = 0.5f;

    private static readonly CreepTypeStats SmallStats = new CreepTypeStats(
        CreepType.Small, SMALL_SPEED, SMALL_DAMAGE_TO_BASE, SMALL_MAX_HEALTH, SMALL_COIN_REWARD);

    private static readonly CreepTypeStats BigStats = new CreepTypeStats(
        CreepType.Big, BIG_SPEED, BIG_DAMAGE_TO_BASE, BIG_MAX_HEALTH, BIG_COIN_REWARD);

    private static readonly IReadOnlyDictionary<CreepType, CreepTypeStats> StatsByType =
        new Dictionary<CreepType, CreepTypeStats>
        {
            { CreepType.Small, SmallStats },
            { CreepType.Big, BigStats }
        };

    private CreepStore creepStore;
    private WaveStore waveStore;
    private Vector3[] singleSpawnPoint;
    private Vector3 basePosition;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        waveStore = new WaveStore();
        singleSpawnPoint = new[] { new Vector3(10f, 0f, 0f) };
        basePosition = Vector3.zero;
    }

    private SpawnSystem MakeSpawnSystem(Vector3[] spawnPositions = null)
    {
        return new SpawnSystem(
            creepStore,
            waveStore,
            spawnPositions ?? singleSpawnPoint,
            basePosition,
            StatsByType);
    }

    private void EnqueueAndSpawn(SpawnSystem system, params CreepType[] types)
    {
        for (int i = 0; i < types.Length; i++)
        {
            waveStore.EnqueueSpawn(types[i]);
        }
        system.Tick(0f);
    }

    // --- Wave-Driven Type Spawning ---

    [Test]
    public void WaveSpawn_SmallType_CreatesSmallCreep()
    {
        var system = MakeSpawnSystem();

        EnqueueAndSpawn(system, CreepType.Small);

        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
    }

    [Test]
    public void WaveSpawn_BigType_CreatesBigCreep()
    {
        var system = MakeSpawnSystem();

        EnqueueAndSpawn(system, CreepType.Big);

        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Big, creepStore.ActiveCreeps[0].Type);
    }

    [Test]
    public void WaveSpawn_MixedTypes_CreatesCorrectTypesInOrder()
    {
        var system = MakeSpawnSystem();

        EnqueueAndSpawn(system, CreepType.Small, CreepType.Big, CreepType.Small);

        Assert.AreEqual(3, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
        Assert.AreEqual(CreepType.Big, creepStore.ActiveCreeps[1].Type);
        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[2].Type);
    }

    [Test]
    public void SpawnedCreeps_HaveCorrectTypeField()
    {
        var system = MakeSpawnSystem();

        EnqueueAndSpawn(system, CreepType.Small, CreepType.Big);

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
