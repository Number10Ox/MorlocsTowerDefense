using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SpawnSystemTests
{
    private CreepStore creepStore;
    private WaveStore waveStore;
    private Vector3[] twoSpawnPoints;
    private Vector3 basePosition;
    private Dictionary<CreepType, CreepTypeStats> statsByType;

    private const float DEFAULT_SPEED = 5f;
    private const int DEFAULT_DAMAGE_TO_BASE = 1;
    private const int DEFAULT_MAX_HEALTH = 3;
    private const int DEFAULT_COIN_REWARD = 1;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        waveStore = new WaveStore();
        twoSpawnPoints = new[] { new Vector3(10f, 0f, 0f), new Vector3(-10f, 0f, 0f) };
        basePosition = Vector3.zero;
        statsByType = new Dictionary<CreepType, CreepTypeStats>
        {
            { CreepType.Small, new CreepTypeStats(CreepType.Small, DEFAULT_SPEED, DEFAULT_DAMAGE_TO_BASE, DEFAULT_MAX_HEALTH, DEFAULT_COIN_REWARD) },
            { CreepType.Big, new CreepTypeStats(CreepType.Big, 2f, 3, 10, 5) }
        };
    }

    private SpawnSystem MakeSystem(
        Vector3[] spawnPositions = null,
        Dictionary<CreepType, CreepTypeStats> stats = null)
    {
        return new SpawnSystem(
            creepStore,
            waveStore,
            spawnPositions ?? twoSpawnPoints,
            basePosition,
            stats ?? statsByType);
    }

    // --- Empty queue ---

    [Test]
    public void Tick_EmptyQueue_NoCreepsSpawned()
    {
        var system = MakeSystem();

        system.Tick(1.0f);

        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
    }

    // --- Single request ---

    [Test]
    public void Tick_SingleRequest_CreatesOneCreep()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_SingleRequest_CreepHasCorrectType()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[0].Type);
    }

    [Test]
    public void Tick_SingleRequest_CreepHasCorrectTarget()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(basePosition, creepStore.ActiveCreeps[0].Target);
    }

    [Test]
    public void Tick_SingleRequest_CreepHasCorrectStats()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        var creep = creepStore.ActiveCreeps[0];
        Assert.AreEqual(DEFAULT_SPEED, creep.Speed, 0.001f);
        Assert.AreEqual(DEFAULT_DAMAGE_TO_BASE, creep.DamageToBase);
        Assert.AreEqual(DEFAULT_MAX_HEALTH, creep.Health);
        Assert.AreEqual(DEFAULT_MAX_HEALTH, creep.MaxHealth);
        Assert.AreEqual(DEFAULT_COIN_REWARD, creep.CoinReward);
    }

    // --- Multiple requests ---

    [Test]
    public void Tick_MultipleRequests_CreatesAll()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Big);
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(3, creepStore.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_MultipleRequests_CreepsHaveUniqueIds()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreNotEqual(creepStore.ActiveCreeps[0].Id, creepStore.ActiveCreeps[1].Id);
    }

    [Test]
    public void Tick_BigCreep_HasBigStats()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Big);

        system.Tick(0f);

        var creep = creepStore.ActiveCreeps[0];
        Assert.AreEqual(CreepType.Big, creep.Type);
        Assert.AreEqual(2f, creep.Speed, 0.001f);
        Assert.AreEqual(3, creep.DamageToBase);
        Assert.AreEqual(10, creep.Health);
        Assert.AreEqual(10, creep.MaxHealth);
        Assert.AreEqual(5, creep.CoinReward);
    }

    // --- Round-robin position assignment ---

    [Test]
    public void Tick_PositionsAssignedRoundRobin()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(twoSpawnPoints[0], creepStore.ActiveCreeps[0].Position);
        Assert.AreEqual(twoSpawnPoints[1], creepStore.ActiveCreeps[1].Position);
        Assert.AreEqual(twoSpawnPoints[0], creepStore.ActiveCreeps[2].Position);
    }

    [Test]
    public void Tick_SingleSpawnPoint_AllCreepsAtSamePosition()
    {
        var singlePoint = new[] { new Vector3(5f, 0f, 0f) };
        var system = MakeSystem(spawnPositions: singlePoint);
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(singlePoint[0], creepStore.ActiveCreeps[0].Position);
        Assert.AreEqual(singlePoint[0], creepStore.ActiveCreeps[1].Position);
    }

    // --- Queue consumption ---

    [Test]
    public void Tick_ConsumesQueue_QueueEmptyAfterTick()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Big);

        system.Tick(0f);

        Assert.AreEqual(0, waveStore.SpawnQueue.Count);
    }

    // --- Unknown type ---

    [Test]
    public void Tick_UnknownCreepType_SkipsCreep()
    {
        var limitedStats = new Dictionary<CreepType, CreepTypeStats>
        {
            { CreepType.Small, new CreepTypeStats(CreepType.Small, DEFAULT_SPEED, DEFAULT_DAMAGE_TO_BASE, DEFAULT_MAX_HEALTH, DEFAULT_COIN_REWARD) }
        };
        var system = MakeSystem(stats: limitedStats);
        waveStore.EnqueueSpawn(CreepType.Big); // Not in stats

        system.Tick(0f);

        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
    }

    // --- No spawn points ---

    [Test]
    public void Tick_NoSpawnPoints_NoCreepsAndQueuePreserved()
    {
        var system = MakeSystem(spawnPositions: new Vector3[0]);
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(1, waveStore.SpawnQueue.Count,
            "Queue should not be consumed when there are no spawn positions");
    }

    // --- SpawnedThisFrame ---

    [Test]
    public void Tick_SpawnedThisFrame_PopulatedAfterSpawn()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);

        system.Tick(0f);

        Assert.AreEqual(1, creepStore.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_SpawnedThisFrame_ClearedByBeginFrame()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);
        system.Tick(0f);
        Assert.AreEqual(1, creepStore.SpawnedThisFrame.Count);

        creepStore.BeginFrame();

        Assert.AreEqual(0, creepStore.SpawnedThisFrame.Count);
    }

    // --- Reset ---

    [Test]
    public void Reset_ResetsCreepIdCounter()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small);
        system.Tick(0f);

        system.Reset();
        waveStore.EnqueueSpawn(CreepType.Small);
        system.Tick(0f);

        // After reset, IDs start from 0 again
        Assert.AreEqual(0, creepStore.ActiveCreeps[1].Id);
    }

    [Test]
    public void Reset_ResetsSpawnIndex()
    {
        var system = MakeSystem();
        waveStore.EnqueueSpawn(CreepType.Small); // position index 0
        system.Tick(0f);

        system.Reset();
        creepStore.BeginFrame();
        waveStore.EnqueueSpawn(CreepType.Small); // should be position index 0 again
        system.Tick(0f);

        Assert.AreEqual(twoSpawnPoints[0], creepStore.ActiveCreeps[0].Position);
    }

    // --- Constructor null checks ---

    [Test]
    public void Constructor_NullCreepStore_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new SpawnSystem(null, waveStore, twoSpawnPoints, basePosition, statsByType));
    }

    [Test]
    public void Constructor_NullWaveStore_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new SpawnSystem(creepStore, null, twoSpawnPoints, basePosition, statsByType));
    }

    [Test]
    public void Constructor_NullSpawnPositions_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new SpawnSystem(creepStore, waveStore, null, basePosition, statsByType));
    }

    [Test]
    public void Constructor_NullStatsByType_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new SpawnSystem(creepStore, waveStore, twoSpawnPoints, basePosition, null));
    }
}
