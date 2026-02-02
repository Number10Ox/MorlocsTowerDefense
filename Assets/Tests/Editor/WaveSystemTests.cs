using System.Collections.Generic;
using NUnit.Framework;

public class WaveSystemTests
{
    private WaveStore waveStore;
    private CreepStore creepStore;

    [SetUp]
    public void SetUp()
    {
        waveStore = new WaveStore();
        creepStore = new CreepStore();
    }

    private WaveSystem MakeSystem(WaveDefinition[] waves)
    {
        return new WaveSystem(waveStore, creepStore, waves);
    }

    private WaveDefinition MakeWave(float delay, params WaveEntryDefinition[] entries)
    {
        return new WaveDefinition(entries, delay);
    }

    private WaveEntryDefinition MakeEntry(CreepType type, int count, float interval = 0.5f)
    {
        return new WaveEntryDefinition(type, count, interval);
    }

    private void SimulateAllCreepsCleared()
    {
        var activeCreeps = creepStore.ActiveCreeps;
        for (int i = 0; i < activeCreeps.Count; i++)
        {
            creepStore.MarkForRemoval(activeCreeps[i].Id);
        }
        creepStore.BeginFrame();
    }

    private void ConsumeQueueAsCreeps(int nextId)
    {
        var buffer = new List<CreepType>();
        waveStore.ConsumeSpawnQueue(buffer);
        for (int i = 0; i < buffer.Count; i++)
        {
            creepStore.Add(new CreepSimData(nextId + i)
            {
                Type = buffer[i],
                Health = 1,
                MaxHealth = 1
            });
        }
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullWaveStore_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new WaveSystem(null, creepStore, new WaveDefinition[0]));
    }

    [Test]
    public void Constructor_NullCreepStore_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new WaveSystem(waveStore, null, new WaveDefinition[0]));
    }

    [Test]
    public void Constructor_NullWaves_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            new WaveSystem(waveStore, creepStore, null));
    }

    // --- Empty waves ---

    [Test]
    public void Constructor_EmptyWaves_AllWavesClearedImmediately()
    {
        MakeSystem(new WaveDefinition[0]);

        Assert.IsTrue(waveStore.AllWavesCleared);
    }

    [Test]
    public void Tick_EmptyWaves_NoSpawns()
    {
        var system = MakeSystem(new WaveDefinition[0]);

        system.Tick(1.0f);

        Assert.AreEqual(0, waveStore.SpawnQueue.Count);
    }

    // --- Delay timing ---

    [Test]
    public void Tick_FirstWaveDelay_NoSpawnsBeforeElapsed()
    {
        var waves = new[]
        {
            MakeWave(2.0f, MakeEntry(CreepType.Small, 5))
        };
        var system = MakeSystem(waves);

        system.Tick(1.0f);

        Assert.AreEqual(0, waveStore.SpawnQueue.Count);
    }

    [Test]
    public void Tick_DelayElapsed_SpawnsStart()
    {
        var waves = new[]
        {
            MakeWave(1.0f, MakeEntry(CreepType.Small, 5, 0.5f))
        };
        var system = MakeSystem(waves);

        system.Tick(1.5f); // 1.0 delay + 0.5 interval = first spawn

        Assert.AreEqual(1, waveStore.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Small, waveStore.SpawnQueue[0]);
    }

    [Test]
    public void Tick_ZeroDelay_SpawnsImmediately()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 3, 0.5f))
        };
        var system = MakeSystem(waves);

        system.Tick(0.5f);

        Assert.AreEqual(1, waveStore.SpawnQueue.Count);
    }

    // --- Spawn cadence ---

    [Test]
    public void Tick_SpawnInterval_EnqueuesAtCorrectCadence()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 5, 1.0f))
        };
        var system = MakeSystem(waves);

        // First tick: 1.0s covers the interval
        system.Tick(1.0f);
        Assert.AreEqual(1, waveStore.SpawnQueue.Count);

        // Second tick: another 1.0s
        system.Tick(1.0f);
        Assert.AreEqual(2, waveStore.SpawnQueue.Count);
    }

    [Test]
    public void Tick_SingleEntry_EnqueuesCorrectTotalCount()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 3, 0.5f))
        };
        var system = MakeSystem(waves);

        // Tick enough time to spawn all 3 (3 * 0.5 = 1.5s)
        system.Tick(1.5f);

        Assert.AreEqual(3, waveStore.SpawnQueue.Count);
    }

    [Test]
    public void Tick_SingleEntry_DoesNotExceedCount()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 2, 0.5f))
        };
        var system = MakeSystem(waves);

        // Tick way more time than needed
        system.Tick(10.0f);

        Assert.AreEqual(2, waveStore.SpawnQueue.Count);
    }

    // --- Multiple entries ---

    [Test]
    public void Tick_MultipleEntries_SpawnsSequentially()
    {
        var waves = new[]
        {
            MakeWave(0f,
                MakeEntry(CreepType.Small, 2, 0.5f),
                MakeEntry(CreepType.Big, 2, 0.5f))
        };
        var system = MakeSystem(waves);

        // Tick enough to spawn all: 2*0.5 + 2*0.5 = 2.0s
        system.Tick(2.0f);

        Assert.AreEqual(4, waveStore.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Small, waveStore.SpawnQueue[0]);
        Assert.AreEqual(CreepType.Small, waveStore.SpawnQueue[1]);
        Assert.AreEqual(CreepType.Big, waveStore.SpawnQueue[2]);
        Assert.AreEqual(CreepType.Big, waveStore.SpawnQueue[3]);
    }

    [Test]
    public void Tick_MixedTypes_CorrectTypePerEntry()
    {
        var waves = new[]
        {
            MakeWave(0f,
                MakeEntry(CreepType.Big, 1, 0.5f),
                MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        system.Tick(1.0f);

        Assert.AreEqual(2, waveStore.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Big, waveStore.SpawnQueue[0]);
        Assert.AreEqual(CreepType.Small, waveStore.SpawnQueue[1]);
    }

    // --- Wave cleared detection ---

    [Test]
    public void Tick_AllCreepsRemoved_WaveCleared()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn 1 creep
        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);

        // Simulate creep killed
        SimulateAllCreepsCleared();

        // WaveSystem detects clear on next tick
        system.Tick(0.1f);

        Assert.IsFalse(waveStore.WaveActive);
    }

    [Test]
    public void Tick_CreepsStillActive_WaveNotCleared()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn and consume into creep store
        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);

        // Don't kill creeps — still active
        system.Tick(0.1f);

        Assert.IsTrue(waveStore.WaveActive);
    }

    [Test]
    public void Tick_QueueNotConsumed_WaveNotCleared()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn but don't consume queue (simulates SpawnSystem hasn't ticked yet)
        system.Tick(0.5f);

        // All wave's creeps spawned, but queue isn't empty
        system.Tick(0.1f);

        // Wave shouldn't be cleared because queue still has items
        Assert.IsTrue(waveStore.WaveActive);
    }

    // --- Inter-wave delay ---

    [Test]
    public void Tick_WaveCleared_NextWaveStartsAfterDelay()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f)),
            MakeWave(2.0f, MakeEntry(CreepType.Big, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn wave 1, consume, kill
        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);
        SimulateAllCreepsCleared();

        // Detect clear + start delay
        system.Tick(0.1f);
        Assert.AreEqual(0, waveStore.CurrentWaveIndex);

        // Partway through delay — no wave 2 spawns yet
        system.Tick(1.0f);
        Assert.AreEqual(0, waveStore.SpawnQueue.Count);

        // After delay elapses + spawn interval
        system.Tick(1.5f);
        Assert.AreEqual(1, waveStore.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Big, waveStore.SpawnQueue[0]);
    }

    [Test]
    public void Tick_WaveCleared_ZeroDelayNextWave_StartsImmediately()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f)),
            MakeWave(0f, MakeEntry(CreepType.Big, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn wave 1, consume, kill
        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);
        SimulateAllCreepsCleared();

        // Detect clear + immediately start wave 2 + enough time for spawn
        system.Tick(0.5f);

        Assert.AreEqual(1, waveStore.CurrentWaveIndex);
        Assert.AreEqual(1, waveStore.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Big, waveStore.SpawnQueue[0]);
    }

    // --- All waves done ---

    [Test]
    public void Tick_LastWaveCleared_AllWavesClearedTrue()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn, consume, kill
        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);
        SimulateAllCreepsCleared();

        // Detect clear
        system.Tick(0.1f);

        Assert.IsTrue(waveStore.AllWavesCleared);
    }

    [Test]
    public void Tick_NotLastWave_AllWavesClearedFalse()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f)),
            MakeWave(0f, MakeEntry(CreepType.Big, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Spawn wave 1, consume, kill
        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);
        SimulateAllCreepsCleared();

        // Detect wave 1 clear — but wave 2 still pending
        system.Tick(0.1f);

        Assert.IsFalse(waveStore.AllWavesCleared);
    }

    // --- Events ---

    [Test]
    public void StartWave_FiresOnWaveStarted()
    {
        int capturedIndex = -1;
        waveStore.OnWaveStarted += index => capturedIndex = index;

        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Zero delay → starts immediately on first tick
        system.Tick(0.5f);

        Assert.AreEqual(0, capturedIndex);
    }

    [Test]
    public void ClearWave_FiresOnWaveCleared()
    {
        int capturedIndex = -1;
        waveStore.OnWaveCleared += index => capturedIndex = index;

        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        system.Tick(0.5f);
        ConsumeQueueAsCreeps(0);
        SimulateAllCreepsCleared();

        system.Tick(0.1f);

        Assert.AreEqual(0, capturedIndex);
    }

    // --- Burst cap ---

    [Test]
    public void Tick_LargeDeltaTime_CapsSpawnsPerTick()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 100, 0.01f))
        };
        var system = MakeSystem(waves);

        // Huge deltaTime — should be capped at MAX_SPAWNS_PER_TICK (20)
        system.Tick(100.0f);

        Assert.LessOrEqual(waveStore.SpawnQueue.Count, 20);
    }

    // --- Wave with empty entries ---

    [Test]
    public void Tick_WaveWithNullEntries_TransitionsToWaitingForClear()
    {
        var waves = new[]
        {
            MakeWave(0f) // No entries
        };
        var system = MakeSystem(waves);

        // Start the wave (zero delay) — should transition immediately past spawning
        system.Tick(0.1f);

        // No creeps in store and no queue → wave should clear
        system.Tick(0.1f);

        Assert.IsTrue(waveStore.AllWavesCleared);
    }

    // --- Reset ---

    [Test]
    public void Reset_EmptyWaves_AllWavesClearedRestored()
    {
        var system = MakeSystem(new WaveDefinition[0]);
        Assert.IsTrue(waveStore.AllWavesCleared);

        // Reset both stores and system (mirrors GameSession.Reset() + system Reset)
        waveStore.Reset();
        Assert.IsFalse(waveStore.AllWavesCleared, "WaveStore.Reset should clear AllWavesCleared");

        system.Reset();
        Assert.IsTrue(waveStore.AllWavesCleared,
            "WaveSystem.Reset should re-mark AllWavesCleared for empty waves");
    }

    [Test]
    public void Reset_ResetsAllState()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 2, 0.5f)),
            MakeWave(0f, MakeEntry(CreepType.Big, 2, 0.5f))
        };
        var system = MakeSystem(waves);

        // Progress through wave 1
        system.Tick(1.0f);
        ConsumeQueueAsCreeps(0);
        SimulateAllCreepsCleared();
        system.Tick(0.1f);

        // Reset
        waveStore.Reset();
        creepStore.Reset();
        system.Reset();

        // Should start from wave 0 again
        system.Tick(0.5f);

        Assert.AreEqual(1, waveStore.SpawnQueue.Count);
        Assert.AreEqual(CreepType.Small, waveStore.SpawnQueue[0]);
    }

    // --- Full multi-wave progression ---

    [Test]
    public void FullProgression_TwoWaves_BothComplete()
    {
        var waves = new[]
        {
            MakeWave(0f, MakeEntry(CreepType.Small, 2, 0.5f)),
            MakeWave(0f, MakeEntry(CreepType.Big, 1, 0.5f))
        };
        var system = MakeSystem(waves);

        // Wave 1: spawn 2 small creeps
        system.Tick(1.0f);
        Assert.AreEqual(2, waveStore.SpawnQueue.Count);
        ConsumeQueueAsCreeps(0);

        // Kill all wave 1 creeps
        SimulateAllCreepsCleared();

        // Detect wave 1 clear, start wave 2 (zero delay), spawn big creep
        system.Tick(0.5f);
        Assert.AreEqual(CreepType.Big, waveStore.SpawnQueue[0]);
        ConsumeQueueAsCreeps(10);

        // Kill wave 2 creeps
        SimulateAllCreepsCleared();

        // Detect wave 2 clear → all waves done
        system.Tick(0.1f);

        Assert.IsTrue(waveStore.AllWavesCleared);
    }
}
