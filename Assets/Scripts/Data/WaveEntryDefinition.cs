using System;
using UnityEngine;

// Per-entry within a wave: which creep type, how many, and spawn cadence.
// Serialized as entries inside WaveDefinition. count = total creeps (scene-independent).
[Serializable]
public struct WaveEntryDefinition
{
    [SerializeField] private CreepType creepType;

    [Tooltip("Total number of creeps to spawn for this entry.")]
    [Min(1)] [SerializeField] private int count;

    [Tooltip("Seconds between individual creep spawns within this entry.")]
    [Min(0.01f)] [SerializeField] private float spawnInterval;

    public CreepType CreepType => creepType;
    public int Count => count;
    public float SpawnInterval => spawnInterval;

    public WaveEntryDefinition(CreepType creepType, int count, float spawnInterval)
    {
        this.creepType = creepType;
        this.count = count;
        this.spawnInterval = spawnInterval;
    }

    public void Validate()
    {
        count = Mathf.Max(1, count);
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
    }
}
