using NUnit.Framework;
using UnityEngine;

public class SpawnSystemTests
{
    private CreepStore store;
    private Vector3[] twoSpawnPoints;
    private Vector3 basePosition;

    private const float DEFAULT_SPEED = 5f;
    private const float DEFAULT_INTERVAL = 1f;
    private const int DEFAULT_PER_SPAWN = 1;
    private const int DEFAULT_DAMAGE_TO_BASE = 1;

    [SetUp]
    public void SetUp()
    {
        store = new CreepStore();
        twoSpawnPoints = new[] { new Vector3(10f, 0f, 0f), new Vector3(-10f, 0f, 0f) };
        basePosition = Vector3.zero;
    }

    private SpawnSystem MakeSystem(
        Vector3[] spawnPositions = null,
        float interval = DEFAULT_INTERVAL,
        int perSpawn = DEFAULT_PER_SPAWN,
        float speed = DEFAULT_SPEED,
        int damageToBase = DEFAULT_DAMAGE_TO_BASE)
    {
        return new SpawnSystem(
            store,
            spawnPositions ?? twoSpawnPoints,
            basePosition,
            interval,
            perSpawn,
            speed,
            damageToBase);
    }

    [Test]
    public void Tick_BeforeIntervalElapsed_NoCreepsSpawned()
    {
        var system = MakeSystem();

        system.Tick(0.5f);

        Assert.AreEqual(0, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_IntervalElapsed_SpawnsCreepsAtAllSpawnPoints()
    {
        var system = MakeSystem();

        system.Tick(1.0f);

        Assert.AreEqual(2, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_IntervalElapsed_CreepsHaveCorrectPositions()
    {
        var system = MakeSystem();

        system.Tick(1.0f);

        Assert.AreEqual(twoSpawnPoints[0], store.ActiveCreeps[0].Position);
        Assert.AreEqual(twoSpawnPoints[1], store.ActiveCreeps[1].Position);
    }

    [Test]
    public void Tick_IntervalElapsed_CreepsHaveCorrectTarget()
    {
        var system = MakeSystem();

        system.Tick(1.0f);

        Assert.AreEqual(basePosition, store.ActiveCreeps[0].Target);
        Assert.AreEqual(basePosition, store.ActiveCreeps[1].Target);
    }

    [Test]
    public void Tick_IntervalElapsed_CreepsHaveCorrectSpeed()
    {
        var system = MakeSystem(speed: 7f);

        system.Tick(1.0f);

        Assert.AreEqual(7f, store.ActiveCreeps[0].Speed, 0.001f);
    }

    [Test]
    public void Tick_IntervalElapsed_CreepsHaveUniqueIds()
    {
        var system = MakeSystem();

        system.Tick(1.0f);

        Assert.AreNotEqual(store.ActiveCreeps[0].Id, store.ActiveCreeps[1].Id);
    }

    [Test]
    public void Tick_MultipleIntervalsElapsed_SpawnsMultipleBursts()
    {
        var system = MakeSystem();

        system.Tick(2.5f);

        Assert.AreEqual(4, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_CreepsPerSpawnGreaterThanOne_SpawnsCorrectCount()
    {
        var system = MakeSystem(perSpawn: 3);

        system.Tick(1.0f);

        Assert.AreEqual(6, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_SpawnedThisFrame_PopulatedAfterSpawn()
    {
        var system = MakeSystem();

        system.Tick(1.0f);

        Assert.AreEqual(2, store.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_SpawnedThisFrame_EmptyOnNonSpawnTick()
    {
        var system = MakeSystem();

        system.Tick(1.0f);
        store.BeginFrame();
        system.Tick(0.1f);

        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_SpawnedThisFrame_ClearedByBeginFrame()
    {
        var system = MakeSystem();

        system.Tick(1.0f);
        Assert.AreEqual(2, store.SpawnedThisFrame.Count);

        store.BeginFrame();

        Assert.AreEqual(0, store.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_NoSpawnPoints_DoesNotThrow()
    {
        var system = MakeSystem(spawnPositions: new Vector3[0]);

        Assert.DoesNotThrow(() => system.Tick(1.0f));
        Assert.AreEqual(0, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_ZeroInterval_DoesNotSpawn()
    {
        var system = MakeSystem(interval: 0f);

        Assert.DoesNotThrow(() => system.Tick(1.0f));
        Assert.AreEqual(0, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_NegativeInterval_DoesNotSpawn()
    {
        var system = MakeSystem(interval: -1f);

        Assert.DoesNotThrow(() => system.Tick(1.0f));
        Assert.AreEqual(0, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_LargeDeltaTime_CappedAtMaxBurstsPerTick()
    {
        // interval=0.5, deltaTime=3.0 → 6 bursts needed, but capped at 5
        var system = MakeSystem(interval: 0.5f);

        system.Tick(3.0f);

        // 5 bursts × 2 spawn points × 1 per spawn = 10
        Assert.AreEqual(10, store.ActiveCreeps.Count);
    }

    [Test]
    public void Tick_IntervalElapsed_CreepsHaveCorrectDamageToBase()
    {
        var system = MakeSystem(damageToBase: 5);

        system.Tick(1.0f);

        Assert.AreEqual(5, store.ActiveCreeps[0].DamageToBase);
        Assert.AreEqual(5, store.ActiveCreeps[1].DamageToBase);
    }
}
