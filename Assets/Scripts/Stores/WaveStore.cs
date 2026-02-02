using System;
using System.Collections.Generic;

// Authoritative wave progress state. Single writer: WaveSystem.
// Readers: SpawnSystem (spawn queue), PlayingState (AllWavesCleared).
public class WaveStore
{
    private readonly List<CreepType> spawnQueue = new();
    private int currentWaveIndex = -1;
    private bool allWavesCleared;
    private bool waveActive;

    public IReadOnlyList<CreepType> SpawnQueue => spawnQueue;
    public int CurrentWaveIndex => currentWaveIndex;
    public bool AllWavesCleared => allWavesCleared;
    public bool WaveActive => waveActive;

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCleared;

    public void EnqueueSpawn(CreepType type)
    {
        spawnQueue.Add(type);
    }

    public void ConsumeSpawnQueue(List<CreepType> destination)
    {
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            destination.Add(spawnQueue[i]);
        }
        spawnQueue.Clear();
    }

    public void StartWave(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        waveActive = true;
        OnWaveStarted?.Invoke(waveIndex);
    }

    public void ClearWave(int waveIndex)
    {
        waveActive = false;
        OnWaveCleared?.Invoke(waveIndex);
    }

    public void MarkAllWavesCleared()
    {
        allWavesCleared = true;
    }

    public void BeginFrame()
    {
        // No per-frame state to clear. Spawn queue is consumed by SpawnSystem during its Tick.
    }

    public void Reset()
    {
        spawnQueue.Clear();
        currentWaveIndex = -1;
        allWavesCleared = false;
        waveActive = false;
    }
}
