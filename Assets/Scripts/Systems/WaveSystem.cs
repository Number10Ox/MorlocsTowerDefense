using System;

// Wave progression system. Enqueues CreepType spawn requests into WaveStore based on
// data-driven wave definitions. Detects wave-cleared via CreepStore.ActiveCreeps.Count.
// Ticks first in Phase 1 (before SpawnSystem).
public class WaveSystem : IGameSystem
{
    private const int MAX_SPAWNS_PER_TICK = 20;

    private enum Phase
    {
        WaitingToStart,
        Spawning,
        WaitingForClear,
        Done
    }

    private readonly WaveStore waveStore;
    private readonly CreepStore creepStore;
    private readonly WaveDefinition[] waves;

    private Phase phase;
    private int currentWaveIdx;
    private int currentEntryIdx;
    private int creepsSpawnedInEntry;
    private float spawnTimer;
    private float delayTimer;

    public WaveSystem(WaveStore waveStore, CreepStore creepStore, WaveDefinition[] waves)
    {
        this.waveStore = waveStore ?? throw new ArgumentNullException(nameof(waveStore));
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.waves = waves ?? throw new ArgumentNullException(nameof(waves));

        if (waves.Length == 0)
        {
            phase = Phase.Done;
            waveStore.MarkAllWavesCleared();
            return;
        }

        phase = Phase.WaitingToStart;
        currentWaveIdx = 0;
        delayTimer = waves[0].DelayBeforeStart;
    }

    public void Tick(float deltaTime)
    {
        if (phase == Phase.Done)
        {
            return;
        }

        float remaining = deltaTime;

        if (phase == Phase.WaitingForClear)
        {
            TickWaitingForClear();
            if (phase != Phase.WaitingToStart && phase != Phase.Done)
            {
                return;
            }
        }

        if (phase == Phase.WaitingToStart)
        {
            remaining = TickWaitingToStart(remaining);
            if (phase != Phase.Spawning)
            {
                return;
            }
        }

        if (phase == Phase.Spawning)
        {
            TickSpawning(remaining);
        }
    }

    public void Reset()
    {
        if (waves.Length == 0)
        {
            phase = Phase.Done;
            waveStore.MarkAllWavesCleared();
            return;
        }

        phase = Phase.WaitingToStart;
        currentWaveIdx = 0;
        currentEntryIdx = 0;
        creepsSpawnedInEntry = 0;
        spawnTimer = 0f;
        delayTimer = waves[0].DelayBeforeStart;
    }

    private void TickWaitingForClear()
    {
        if (waveStore.SpawnQueue.Count > 0 || creepStore.ActiveCreeps.Count > 0)
        {
            return;
        }

        waveStore.ClearWave(currentWaveIdx);

        currentWaveIdx++;
        if (currentWaveIdx >= waves.Length)
        {
            waveStore.MarkAllWavesCleared();
            phase = Phase.Done;
            return;
        }

        delayTimer = waves[currentWaveIdx].DelayBeforeStart;
        phase = Phase.WaitingToStart;
    }

    private float TickWaitingToStart(float deltaTime)
    {
        delayTimer -= deltaTime;
        if (delayTimer > 0f)
        {
            return 0f;
        }

        float excess = -delayTimer;
        StartCurrentWave();
        return excess;
    }

    private void StartCurrentWave()
    {
        currentEntryIdx = 0;
        creepsSpawnedInEntry = 0;
        spawnTimer = 0f;

        waveStore.StartWave(currentWaveIdx);

        WaveDefinition wave = waves[currentWaveIdx];
        if (wave.Entries == null || wave.Entries.Length == 0)
        {
            phase = Phase.WaitingForClear;
            return;
        }

        phase = Phase.Spawning;
    }

    private void TickSpawning(float deltaTime)
    {
        WaveDefinition wave = waves[currentWaveIdx];
        WaveEntryDefinition entry = wave.Entries[currentEntryIdx];

        spawnTimer += deltaTime;

        int spawnsThisTick = 0;
        while (spawnTimer >= entry.SpawnInterval && spawnsThisTick < MAX_SPAWNS_PER_TICK)
        {
            spawnTimer -= entry.SpawnInterval;
            waveStore.EnqueueSpawn(entry.CreepType);
            creepsSpawnedInEntry++;
            spawnsThisTick++;

            if (creepsSpawnedInEntry >= entry.Count)
            {
                if (!AdvanceToNextEntry(wave))
                {
                    return;
                }
                entry = wave.Entries[currentEntryIdx];
            }
        }

        // Clamp leftover to prevent unbounded accumulation after long stalls
        if (spawnTimer > entry.SpawnInterval)
        {
            spawnTimer = entry.SpawnInterval;
        }
    }

    private bool AdvanceToNextEntry(WaveDefinition wave)
    {
        currentEntryIdx++;
        creepsSpawnedInEntry = 0;

        if (currentEntryIdx >= wave.Entries.Length)
        {
            phase = Phase.WaitingForClear;
            return false;
        }

        // Carry over leftover time into the new entry's interval
        return true;
    }
}
