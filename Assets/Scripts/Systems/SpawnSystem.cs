using System;
using System.Collections.Generic;
using UnityEngine;

// Queue-driven creep spawner. Consumes CreepType requests from WaveStore.SpawnQueue,
// looks up stats, assigns spawn positions round-robin, and creates CreepSimData in CreepStore.
public class SpawnSystem : IGameSystem
{
    private readonly CreepStore creepStore;
    private readonly WaveStore waveStore;
    private readonly Vector3[] spawnPositions;
    private readonly Vector3 basePosition;
    private readonly IReadOnlyDictionary<CreepType, CreepTypeStats> statsByType;
    private readonly List<CreepType> consumeBuffer = new();

    private int nextCreepId;
    private int currentSpawnIndex;

    public SpawnSystem(
        CreepStore creepStore,
        WaveStore waveStore,
        Vector3[] spawnPositions,
        Vector3 basePosition,
        IReadOnlyDictionary<CreepType, CreepTypeStats> statsByType)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.waveStore = waveStore ?? throw new ArgumentNullException(nameof(waveStore));
        this.spawnPositions = spawnPositions ?? throw new ArgumentNullException(nameof(spawnPositions));
        this.basePosition = basePosition;
        this.statsByType = statsByType ?? throw new ArgumentNullException(nameof(statsByType));
    }

    public void Tick(float deltaTime)
    {
        if (spawnPositions.Length == 0)
        {
            return;
        }

        consumeBuffer.Clear();
        waveStore.ConsumeSpawnQueue(consumeBuffer);

        if (consumeBuffer.Count == 0)
        {
            return;
        }

        for (int i = 0; i < consumeBuffer.Count; i++)
        {
            CreepType type = consumeBuffer[i];

            if (!statsByType.TryGetValue(type, out CreepTypeStats stats))
            {
                Debug.LogWarning($"SpawnSystem: No stats found for CreepType {type}. Skipping.");
                continue;
            }

            Vector3 position = spawnPositions[currentSpawnIndex];
            currentSpawnIndex = (currentSpawnIndex + 1) % spawnPositions.Length;

            var creep = new CreepSimData(nextCreepId++)
            {
                Type = stats.Type,
                Position = position,
                Target = basePosition,
                Speed = stats.Speed,
                DamageToBase = stats.DamageToBase,
                Health = stats.MaxHealth,
                MaxHealth = stats.MaxHealth,
                CoinReward = stats.CoinReward
            };
            creepStore.Add(creep);
        }
    }

    public void Reset()
    {
        nextCreepId = 0;
        currentSpawnIndex = 0;
    }
}
