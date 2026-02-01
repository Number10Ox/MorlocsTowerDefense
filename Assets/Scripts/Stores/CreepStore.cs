using System;
using System.Collections.Generic;

// Authoritative creep collection. Writers: SpawnSystem (Add), MovementSystem/DamageSystem (MarkForRemoval).
// Exposes per-frame change lists.
public class CreepStore
{
    private readonly List<CreepSimData> activeCreeps = new();
    private readonly HashSet<int> pendingRemovals = new();
    private readonly List<CreepSimData> spawnedThisFrame = new();
    private readonly List<int> removedIdsThisFrame = new();

    public IReadOnlyList<CreepSimData> ActiveCreeps => activeCreeps;
    public IReadOnlyList<CreepSimData> SpawnedThisFrame => spawnedThisFrame;
    public IReadOnlyList<int> RemovedIdsThisFrame => removedIdsThisFrame;

    // Clears per-frame change lists and applies deferred removals from the prior frame.
    public void BeginFrame()
    {
        spawnedThisFrame.Clear();
        removedIdsThisFrame.Clear();
        FlushRemovals();
    }

    public void Add(CreepSimData creep)
    {
        if (creep == null)
        {
            throw new ArgumentNullException(nameof(creep));
        }

        activeCreeps.Add(creep);
        spawnedThisFrame.Add(creep);
    }

    public void MarkForRemoval(int creepId)
    {
        pendingRemovals.Add(creepId);
    }

    // Read-only lookup. Callers must not mutate fields they do not own.
    public bool TryGetCreep(int creepId, out CreepSimData creep)
    {
        for (int i = 0; i < activeCreeps.Count; i++)
        {
            if (activeCreeps[i].Id == creepId)
            {
                creep = activeCreeps[i];
                return true;
            }
        }

        creep = null;
        return false;
    }

    public void Reset()
    {
        activeCreeps.Clear();
        pendingRemovals.Clear();
        spawnedThisFrame.Clear();
        removedIdsThisFrame.Clear();
    }

    private void FlushRemovals()
    {
        foreach (int id in pendingRemovals)
        {
            for (int i = activeCreeps.Count - 1; i >= 0; i--)
            {
                if (activeCreeps[i].Id == id)
                {
                    activeCreeps.RemoveAt(i);
                    removedIdsThisFrame.Add(id);
                    break;
                }
            }
        }

        pendingRemovals.Clear();
    }
}
