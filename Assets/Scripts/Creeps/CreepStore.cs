using System;
using System.Collections.Generic;

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
