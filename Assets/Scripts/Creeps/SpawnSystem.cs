using System;
using UnityEngine;

public class SpawnSystem : IGameSystem
{
    private const int MAX_BURSTS_PER_TICK = 5;

    private readonly CreepStore creepStore;
    private readonly Vector3[] spawnPositions;
    private readonly Vector3 basePosition;
    private readonly float spawnInterval;
    private readonly int creepsPerSpawn;
    private readonly float creepSpeed;

    private float spawnTimer;
    private int nextCreepId;

    public SpawnSystem(
        CreepStore creepStore,
        Vector3[] spawnPositions,
        Vector3 basePosition,
        float spawnInterval,
        int creepsPerSpawn,
        float creepSpeed)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.spawnPositions = spawnPositions ?? throw new ArgumentNullException(nameof(spawnPositions));
        this.basePosition = basePosition;
        this.spawnInterval = spawnInterval;
        this.creepsPerSpawn = creepsPerSpawn;
        this.creepSpeed = creepSpeed;
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
    }

    private void SpawnBurst()
    {
        for (int s = 0; s < spawnPositions.Length; s++)
        {
            for (int c = 0; c < creepsPerSpawn; c++)
            {
                var creep = new CreepSimData(nextCreepId++)
                {
                    Position = spawnPositions[s],
                    Target = basePosition,
                    Speed = creepSpeed
                };
                creepStore.Add(creep);
            }
        }
    }
}
