using System;
using System.Collections.Generic;

// Authoritative projectile collection with deferred removal.
// HitsThisFrame bridges ProjectileSystem to DamageSystem.
public class ProjectileStore
{
    private readonly List<ProjectileSimData> activeProjectiles = new();
    private readonly HashSet<int> pendingRemovals = new();
    private readonly List<ProjectileSimData> spawnedThisFrame = new();
    private readonly List<int> removedIdsThisFrame = new();
    private readonly List<ProjectileHit> hitsThisFrame = new();

    public IReadOnlyList<ProjectileSimData> ActiveProjectiles => activeProjectiles;
    public IReadOnlyList<ProjectileSimData> SpawnedThisFrame => spawnedThisFrame;
    public IReadOnlyList<int> RemovedIdsThisFrame => removedIdsThisFrame;
    public IReadOnlyList<ProjectileHit> HitsThisFrame => hitsThisFrame;

    public void BeginFrame()
    {
        spawnedThisFrame.Clear();
        removedIdsThisFrame.Clear();
        hitsThisFrame.Clear();
        FlushRemovals();
    }

    public void Add(ProjectileSimData projectile)
    {
        if (projectile == null)
        {
            throw new ArgumentNullException(nameof(projectile));
        }

        activeProjectiles.Add(projectile);
        spawnedThisFrame.Add(projectile);
    }

    public void MarkForRemoval(int projectileId)
    {
        pendingRemovals.Add(projectileId);
    }

    public void RecordHit(ProjectileHit hit)
    {
        hitsThisFrame.Add(hit);
    }

    public void Reset()
    {
        activeProjectiles.Clear();
        pendingRemovals.Clear();
        spawnedThisFrame.Clear();
        removedIdsThisFrame.Clear();
        hitsThisFrame.Clear();
    }

    private void FlushRemovals()
    {
        foreach (int id in pendingRemovals)
        {
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                if (activeProjectiles[i].Id == id)
                {
                    activeProjectiles.RemoveAt(i);
                    removedIdsThisFrame.Add(id);
                    break;
                }
            }
        }

        pendingRemovals.Clear();
    }
}
