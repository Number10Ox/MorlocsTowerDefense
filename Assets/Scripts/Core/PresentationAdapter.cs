using System;
using System.Collections.Generic;
using UnityEngine;
using ObjectPooling;

public class PresentationAdapter
{
    private const int INITIAL_CREEP_MAP_CAPACITY = 32;

    private readonly CreepStore creepStore;
    private readonly GameObjectPool creepPool;
    private readonly Dictionary<int, CreepComponent> creepMap;

    public PresentationAdapter(CreepStore creepStore, GameObjectPool creepPool)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.creepPool = creepPool ?? throw new ArgumentNullException(nameof(creepPool));
        creepMap = new Dictionary<int, CreepComponent>(INITIAL_CREEP_MAP_CAPACITY);
    }

    public void CollectInput()
    {
    }

    public void SyncVisuals()
    {
        ProcessRemovals();
        ProcessSpawns();
        UpdatePositions();
    }

    private void ProcessRemovals()
    {
        var removed = creepStore.RemovedIdsThisFrame;
        for (int i = 0; i < removed.Count; i++)
        {
            int id = removed[i];
            if (creepMap.TryGetValue(id, out CreepComponent comp))
            {
                creepPool.Return(comp.gameObject);
                creepMap.Remove(id);
            }
        }
    }

    private void ProcessSpawns()
    {
        var spawned = creepStore.SpawnedThisFrame;
        for (int i = 0; i < spawned.Count; i++)
        {
            CreepSimData creep = spawned[i];
            GameObject go = creepPool.Get(creep.Position);
            if (go.TryGetComponent(out CreepComponent comp))
            {
                if (creepMap.ContainsKey(creep.Id))
                {
                    Debug.LogWarning($"PresentationAdapter: Duplicate creep Id={creep.Id}. Overwriting visual binding.");
                }

                comp.Initialize(creep.Id);
                creepMap[creep.Id] = comp;
            }
            else
            {
                Debug.LogError($"PresentationAdapter: Creep prefab is missing CreepComponent. Id={creep.Id}");
                creepPool.Return(go);
            }
        }
    }

    private void UpdatePositions()
    {
        var active = creepStore.ActiveCreeps;
        for (int i = 0; i < active.Count; i++)
        {
            CreepSimData creep = active[i];
            if (creepMap.TryGetValue(creep.Id, out CreepComponent comp))
            {
                comp.transform.position = creep.Position;
            }
        }
    }
}
