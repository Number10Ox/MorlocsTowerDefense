using System;
using UnityEngine;

// Timer-driven creep spawner. Creates CreepSimData in CreepStore each burst interval.
// Cycles through creep types round-robin per creep using orderedStats from CreepTypeDirectory.
public class SpawnSystem : IGameSystem
{
    private const int MAX_BURSTS_PER_TICK = 5;

    private readonly CreepStore creepStore;
    private readonly Vector3[] spawnPositions;
    private readonly Vector3 basePosition;
    private readonly float spawnInterval;
    private readonly int creepsPerSpawn;
    private readonly CreepTypeStats[] orderedStats;

    private float spawnTimer;
    private int nextCreepId;
    private int currentTypeIndex;

    public SpawnSystem(
        CreepStore creepStore,
        Vector3[] spawnPositions,
        Vector3 basePosition,
        float spawnInterval,
        int creepsPerSpawn,
        CreepTypeStats[] orderedStats)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.spawnPositions = spawnPositions ?? throw new ArgumentNullException(nameof(spawnPositions));
        this.basePosition = basePosition;
        this.spawnInterval = spawnInterval;
        this.creepsPerSpawn = creepsPerSpawn;
        this.orderedStats = orderedStats ?? throw new ArgumentNullException(nameof(orderedStats));
    }

    public void Tick(float deltaTime)
    {
        if (spawnPositions.Length == 0)
        {
            return;
        }

        if (spawnInterval <= 0f)
        {
            return;
        }

        if (creepsPerSpawn <= 0)
        {
            return;
        }

        if (orderedStats.Length == 0)
        {
            return;
        }

        spawnTimer += deltaTime;

        int bursts = 0;
        while (spawnTimer >= spawnInterval && bursts < MAX_BURSTS_PER_TICK)
        {
            spawnTimer -= spawnInterval;
            SpawnBurst();
            bursts++;
        }

        // Clamp leftover timer to prevent unbounded accumulation after long stalls
        if (spawnTimer > spawnInterval)
        {
            spawnTimer = spawnInterval;
        }
    }

    public void Reset()
    {
        spawnTimer = 0f;
        nextCreepId = 0;
        currentTypeIndex = 0;
    }

    private void SpawnBurst()
    {
        for (int s = 0; s < spawnPositions.Length; s++)
        {
            for (int c = 0; c < creepsPerSpawn; c++)
            {
                CreepTypeStats stats = orderedStats[currentTypeIndex];
                var creep = new CreepSimData(nextCreepId++)
                {
                    Type = stats.Type,
                    Position = spawnPositions[s],
                    Target = basePosition,
                    Speed = stats.Speed,
                    DamageToBase = stats.DamageToBase,
                    Health = stats.MaxHealth,
                    MaxHealth = stats.MaxHealth,
                    CoinReward = stats.CoinReward
                };
                creepStore.Add(creep);
                currentTypeIndex = (currentTypeIndex + 1) % orderedStats.Length;
            }
        }
    }
}
