using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ObjectPooling;

public class PresentationAdapter
{
    private const int INITIAL_CREEP_MAP_CAPACITY = 32;
    private const int INITIAL_TURRET_MAP_CAPACITY = 16;

    private readonly CreepStore creepStore;
    private readonly GameObjectPool creepPool;
    private readonly Dictionary<int, CreepComponent> creepMap;

    private readonly TurretStore turretStore;
    private readonly GameObjectPool turretPool;
    private readonly Dictionary<int, TurretComponent> turretMap;

    private readonly PlacementInput placementInput;
    private readonly Camera camera;
    private readonly LayerMask terrainLayerMask;

    public PresentationAdapter(
        CreepStore creepStore,
        GameObjectPool creepPool,
        TurretStore turretStore,
        GameObjectPool turretPool,
        PlacementInput placementInput,
        Camera camera,
        LayerMask terrainLayerMask)
    {
        this.creepStore = creepStore ?? throw new ArgumentNullException(nameof(creepStore));
        this.creepPool = creepPool ?? throw new ArgumentNullException(nameof(creepPool));
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.turretPool = turretPool ?? throw new ArgumentNullException(nameof(turretPool));
        this.placementInput = placementInput ?? throw new ArgumentNullException(nameof(placementInput));
        this.camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
        this.terrainLayerMask = terrainLayerMask;

        creepMap = new Dictionary<int, CreepComponent>(INITIAL_CREEP_MAP_CAPACITY);
        turretMap = new Dictionary<int, TurretComponent>(INITIAL_TURRET_MAP_CAPACITY);
    }

    public void CollectInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;

        Vector2 screenPos = mouse.position.ReadValue();
        Ray ray = camera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayerMask))
        {
            placementInput.PlaceRequested = true;
            placementInput.WorldPosition = hit.point;
        }
    }

    public void SyncVisuals()
    {
        ProcessCreepRemovals();
        ProcessCreepSpawns();
        UpdateCreepPositions();
        ProcessTurretSpawns();
    }

    private void ProcessCreepRemovals()
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

    private void ProcessCreepSpawns()
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

    private void UpdateCreepPositions()
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

    private void ProcessTurretSpawns()
    {
        var placed = turretStore.PlacedThisFrame;
        for (int i = 0; i < placed.Count; i++)
        {
            TurretSimData turret = placed[i];
            GameObject go = turretPool.Get(turret.Position);
            if (go.TryGetComponent(out TurretComponent comp))
            {
                if (turretMap.ContainsKey(turret.Id))
                {
                    Debug.LogWarning($"PresentationAdapter: Duplicate turret Id={turret.Id}. Overwriting visual binding.");
                }

                comp.Initialize(turret.Id);
                turretMap[turret.Id] = comp;
            }
            else
            {
                Debug.LogError($"PresentationAdapter: Turret prefab is missing TurretComponent. Id={turret.Id}");
                turretPool.Return(go);
            }
        }
    }
}
