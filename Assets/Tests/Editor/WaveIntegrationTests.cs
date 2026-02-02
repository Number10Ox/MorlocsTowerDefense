using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Integration tests: WaveSystem + SpawnSystem + CreepStore + WaveStore working together.
// Verifies full lifecycle from wave enqueue through spawn creation to wave-cleared detection.
public class WaveIntegrationTests
{
    private WaveStore waveStore;
    private CreepStore creepStore;
    private WaveSystem waveSystem;
    private SpawnSystem spawnSystem;

    private static readonly Vector3 BASE_POSITION = new(10f, 0f, 10f);
    private static readonly Vector3[] SPAWN_POSITIONS = new[]
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(5f, 0f, 0f)
    };

    private static readonly IReadOnlyDictionary<CreepType, CreepTypeStats> STATS_BY_TYPE =
        new Dictionary<CreepType, CreepTypeStats>
        {
            { CreepType.Small, new CreepTypeStats(CreepType.Small, 2f, 1, 10, 5) },
            { CreepType.Big, new CreepTypeStats(CreepType.Big, 1f, 3, 30, 15) }
        };

    private void Setup(WaveDefinition[] waves)
    {
        waveStore = new WaveStore();
        creepStore = new CreepStore();
        waveSystem = new WaveSystem(waveStore, creepStore, waves);
        spawnSystem = new SpawnSystem(creepStore, waveStore, SPAWN_POSITIONS, BASE_POSITION, STATS_BY_TYPE);
    }

    private void TickBoth(float dt)
    {
        creepStore.BeginFrame();
        waveStore.BeginFrame();
        waveSystem.Tick(dt);
        spawnSystem.Tick(dt);
    }

    private void KillAllCreeps()
    {
        var active = creepStore.ActiveCreeps;
        for (int i = 0; i < active.Count; i++)
        {
            creepStore.MarkForRemoval(active[i].Id);
        }
    }

    private WaveDefinition MakeWave(float delay, params WaveEntryDefinition[] entries)
    {
        return new WaveDefinition(entries, delay);
    }

    private WaveEntryDefinition MakeEntry(CreepType type, int count, float interval = 0.5f)
    {
        return new WaveEntryDefinition(type, count, interval);
    }

    // --- Single wave full lifecycle ---

    [Test]
    public void FullWave_SpawnAndClear_WaveCompletes()
    {
        Setup(new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 2, 0.5f))
        });

        // Tick enough to spawn both creeps (2 * 0.5 = 1.0s)
        TickBoth(1.0f);

        Assert.AreEqual(2, creepStore.ActiveCreeps.Count);
        Assert.IsTrue(waveStore.WaveActive);

        // Kill all creeps
        KillAllCreeps();

        // Next tick: BeginFrame flushes removals, WaveSystem detects clear
        TickBoth(0.1f);

        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
        Assert.IsFalse(waveStore.WaveActive);
        Assert.IsTrue(waveStore.AllWavesCleared);
    }

    // --- Multi-wave progression ---

    [Test]
    public void MultiWave_SequentialProgression_AllWavesCleared()
    {
        Setup(new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 2, 0.5f)),
            MakeWave(0f, MakeEntry(CreepType.Big, 1, 0.5f))
        });

        // Wave 1: spawn 2 small creeps
        TickBoth(1.0f);

        Assert.AreEqual(2, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(0, waveStore.CurrentWaveIndex);

        // Verify creeps have correct type
        for (int i = 0; i < creepStore.ActiveCreeps.Count; i++)
        {
            Assert.AreEqual(CreepType.Small, creepStore.ActiveCreeps[i].Type);
        }

        // Kill wave 1 creeps
        KillAllCreeps();

        // Detect clear + start wave 2 (zero delay) + spawn big creep
        TickBoth(0.5f);

        Assert.AreEqual(1, waveStore.CurrentWaveIndex);
        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Big, creepStore.ActiveCreeps[0].Type);

        // Kill wave 2 creeps
        KillAllCreeps();

        // Detect final wave clear
        TickBoth(0.1f);

        Assert.IsTrue(waveStore.AllWavesCleared);
        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
    }

    // --- Creep stats and positions ---

    [Test]
    public void SpawnedCreeps_HaveCorrectStatsAndTarget()
    {
        Setup(new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Big, 1, 0.5f))
        });

        TickBoth(0.5f);

        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        var creep = creepStore.ActiveCreeps[0];
        Assert.AreEqual(CreepType.Big, creep.Type);
        Assert.AreEqual(30, creep.MaxHealth);
        Assert.AreEqual(30, creep.Health);
        Assert.AreEqual(1f, creep.Speed);
        Assert.AreEqual(3, creep.DamageToBase);
        Assert.AreEqual(15, creep.CoinReward);
        Assert.AreEqual(BASE_POSITION, creep.Target);
    }

    [Test]
    public void SpawnedCreeps_PositionsAssignedRoundRobin()
    {
        Setup(new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 4, 0.25f))
        });

        // Spawn all 4 (4 * 0.25 = 1.0s)
        TickBoth(1.0f);

        Assert.AreEqual(4, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(SPAWN_POSITIONS[0], creepStore.ActiveCreeps[0].Position);
        Assert.AreEqual(SPAWN_POSITIONS[1], creepStore.ActiveCreeps[1].Position);
        Assert.AreEqual(SPAWN_POSITIONS[0], creepStore.ActiveCreeps[2].Position);
        Assert.AreEqual(SPAWN_POSITIONS[1], creepStore.ActiveCreeps[3].Position);
    }

    // --- Inter-wave delay ---

    [Test]
    public void MultiWave_WithDelay_RespectsInterWaveTiming()
    {
        Setup(new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f)),
            MakeWave(2.0f, MakeEntry(CreepType.Big, 1, 0.5f))
        });

        // Spawn wave 1
        TickBoth(0.5f);
        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);

        // Kill wave 1
        KillAllCreeps();

        // Detect clear, start delay
        TickBoth(0.1f);
        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
        Assert.IsFalse(waveStore.AllWavesCleared);

        // Partway through delay — no spawns yet
        TickBoth(1.0f);
        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);

        // After delay + spawn interval (remaining ~0.9 delay + 0.5 interval)
        TickBoth(1.5f);
        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(CreepType.Big, creepStore.ActiveCreeps[0].Type);
    }
}
