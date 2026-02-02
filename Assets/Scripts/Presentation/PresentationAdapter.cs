using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ObjectPooling;

// Bridges simulation and Unity scene. Collects input into sim-readable structs,
// reads keyboard for turret type selection (data-driven), syncs entity visuals from store change lists via object pools.
public class PresentationAdapter
{
    public readonly struct StoreDeps
    {
        public readonly CreepStore CreepStore;
        public readonly TurretStore TurretStore;
        public readonly ProjectileStore ProjectileStore;

        public StoreDeps(CreepStore creepStore, TurretStore turretStore, ProjectileStore projectileStore)
        {
            CreepStore = creepStore;
            TurretStore = turretStore;
            ProjectileStore = projectileStore;
        }
    }

    public readonly struct PoolDeps
    {
        public readonly IReadOnlyDictionary<CreepType, GameObjectPool> CreepPoolByType;
        public readonly IReadOnlyDictionary<TurretType, GameObjectPool> TurretPoolByType;
        public readonly GameObjectPool ProjectilePool;

        public PoolDeps(
            IReadOnlyDictionary<CreepType, GameObjectPool> creepPoolByType,
            IReadOnlyDictionary<TurretType, GameObjectPool> turretPoolByType,
            GameObjectPool projectilePool)
        {
            CreepPoolByType = creepPoolByType;
            TurretPoolByType = turretPoolByType;
            ProjectilePool = projectilePool;
        }
    }

    public readonly struct InputDeps
    {
        public readonly PlacementInput PlacementInput;
        public readonly TurretSelectionStore SelectionStore;
        public readonly TurretType[] TurretTypeOrder;

        public InputDeps(PlacementInput placementInput, TurretSelectionStore selectionStore, TurretType[] turretTypeOrder)
        {
            PlacementInput = placementInput;
            SelectionStore = selectionStore;
            TurretTypeOrder = turretTypeOrder;
        }
    }

    public readonly struct SceneDeps
    {
        public readonly Camera Camera;
        public readonly LayerMask TerrainLayerMask;

        public SceneDeps(Camera camera, LayerMask terrainLayerMask)
        {
            Camera = camera;
            TerrainLayerMask = terrainLayerMask;
        }
    }

    private const int INITIAL_CREEP_MAP_CAPACITY = 32;
    private const int INITIAL_TURRET_MAP_CAPACITY = 16;
    private const int INITIAL_PROJECTILE_MAP_CAPACITY = 64;

    private static readonly Key[] DIGIT_KEYS = new Key[]
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    private struct CreepVisual
    {
        public CreepComponent Component;
        public GameObjectPool SourcePool;
    }

    private struct TurretVisual
    {
        public TurretComponent Component;
        public GameObjectPool SourcePool;
    }

    private readonly CreepStore creepStore;
    private readonly IReadOnlyDictionary<CreepType, GameObjectPool> creepPoolByType;
    private readonly Dictionary<int, CreepVisual> creepVisuals;

    private readonly TurretStore turretStore;
    private readonly IReadOnlyDictionary<TurretType, GameObjectPool> turretPoolByType;
    private readonly Dictionary<int, TurretVisual> turretVisuals;

    private readonly ProjectileStore projectileStore;
    private readonly GameObjectPool projectilePool;
    private readonly Dictionary<int, ProjectileComponent> projectileMap;

    private readonly PlacementInput placementInput;
    private readonly TurretSelectionStore selectionStore;
    private readonly TurretType[] turretTypeOrder;
    private readonly Camera camera;
    private readonly LayerMask terrainLayerMask;

    public PresentationAdapter(
        StoreDeps stores,
        PoolDeps pools,
        InputDeps input,
        SceneDeps scene)
    {
        this.creepStore = stores.CreepStore ?? throw new ArgumentNullException(nameof(stores.CreepStore));
        this.turretStore = stores.TurretStore ?? throw new ArgumentNullException(nameof(stores.TurretStore));
        this.projectileStore = stores.ProjectileStore ?? throw new ArgumentNullException(nameof(stores.ProjectileStore));
        this.creepPoolByType = pools.CreepPoolByType ?? throw new ArgumentNullException(nameof(pools.CreepPoolByType));
        this.turretPoolByType = pools.TurretPoolByType ?? throw new ArgumentNullException(nameof(pools.TurretPoolByType));
        this.projectilePool = pools.ProjectilePool ?? throw new ArgumentNullException(nameof(pools.ProjectilePool));
        this.placementInput = input.PlacementInput ?? throw new ArgumentNullException(nameof(input.PlacementInput));
        this.selectionStore = input.SelectionStore ?? throw new ArgumentNullException(nameof(input.SelectionStore));
        this.turretTypeOrder = input.TurretTypeOrder ?? throw new ArgumentNullException(nameof(input.TurretTypeOrder));
        this.camera = scene.Camera ? scene.Camera : throw new ArgumentNullException(nameof(scene.Camera));
        this.terrainLayerMask = scene.TerrainLayerMask;

        creepVisuals = new Dictionary<int, CreepVisual>(INITIAL_CREEP_MAP_CAPACITY);
        turretVisuals = new Dictionary<int, TurretVisual>(INITIAL_TURRET_MAP_CAPACITY);
        projectileMap = new Dictionary<int, ProjectileComponent>(INITIAL_PROJECTILE_MAP_CAPACITY);
    }

    public void CollectInput()
    {
        placementInput.Clear();

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard[Key.R].wasPressedThisFrame)
            {
                placementInput.RestartRequested = true;
            }

            int maxKeys = Math.Min(turretTypeOrder.Length, DIGIT_KEYS.Length);
            for (int i = 0; i < maxKeys; i++)
            {
                if (keyboard[DIGIT_KEYS[i]].wasPressedThisFrame)
                {
                    selectionStore.SelectType(turretTypeOrder[i]);
                    break;
                }
            }
        }

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
        ProcessProjectileRemovals();
        ProcessProjectileSpawns();
        UpdateProjectilePositions();
    }

    public void ResetVisuals()
    {
        ReturnCreepsToSourcePools();
        ReturnTurretsToSourcePools();
        ReturnAllToPool(projectileMap, projectilePool);
    }

    private void ProcessCreepRemovals()
    {
        var removed = creepStore.RemovedIdsThisFrame;
        for (int i = 0; i < removed.Count; i++)
        {
            int id = removed[i];
            if (creepVisuals.TryGetValue(id, out CreepVisual visual))
            {
                visual.SourcePool.Return(visual.Component.gameObject);
                creepVisuals.Remove(id);
            }
        }
    }

    private void ProcessCreepSpawns()
    {
        var spawned = creepStore.SpawnedThisFrame;
        for (int i = 0; i < spawned.Count; i++)
        {
            CreepSimData creep = spawned[i];
            if (!creepPoolByType.TryGetValue(creep.Type, out GameObjectPool pool))
            {
                Debug.LogWarning($"PresentationAdapter: No pool for CreepType {creep.Type}.");
                continue;
            }

            GameObject go = pool.Acquire(creep.Position);
            if (go.TryGetComponent(out CreepComponent comp))
            {
                if (creepVisuals.TryGetValue(creep.Id, out CreepVisual oldVisual))
                {
                    Debug.LogWarning($"PresentationAdapter: Duplicate creep Id={creep.Id}. Returning previous GO.");
                    oldVisual.SourcePool.Return(oldVisual.Component.gameObject);
                }

                comp.Initialize(creep.Id);
                creepVisuals[creep.Id] = new CreepVisual
                {
                    Component = comp,
                    SourcePool = pool
                };
            }
            else
            {
                Debug.LogError($"PresentationAdapter: Creep prefab is missing CreepComponent. Id={creep.Id}");
                pool.Return(go);
            }
        }
    }

    private void UpdateCreepPositions()
    {
        var active = creepStore.ActiveCreeps;
        for (int i = 0; i < active.Count; i++)
        {
            CreepSimData creep = active[i];
            if (creepVisuals.TryGetValue(creep.Id, out CreepVisual visual))
            {
                visual.Component.transform.position = creep.Position;
            }
        }
    }

    private void ProcessTurretSpawns()
    {
        var placed = turretStore.PlacedThisFrame;
        for (int i = 0; i < placed.Count; i++)
        {
            TurretSimData turret = placed[i];
            if (!turretPoolByType.TryGetValue(turret.Type, out GameObjectPool pool))
            {
                Debug.LogWarning($"PresentationAdapter: No pool for TurretType {turret.Type}.");
                continue;
            }
            GameObject go = pool.Acquire(turret.Position);
            if (go.TryGetComponent(out TurretComponent comp))
            {
                if (turretVisuals.TryGetValue(turret.Id, out TurretVisual oldVisual))
                {
                    Debug.LogWarning($"PresentationAdapter: Duplicate turret Id={turret.Id}. Returning previous GO.");
                    oldVisual.SourcePool.Return(oldVisual.Component.gameObject);
                }

                comp.Initialize(turret.Id);
                turretVisuals[turret.Id] = new TurretVisual
                {
                    Component = comp,
                    SourcePool = pool
                };
            }
            else
            {
                Debug.LogError($"PresentationAdapter: Turret prefab is missing TurretComponent. Id={turret.Id}");
                pool.Return(go);
            }
        }
    }

    private void ProcessProjectileRemovals()
    {
        var removed = projectileStore.RemovedIdsThisFrame;
        for (int i = 0; i < removed.Count; i++)
        {
            int id = removed[i];
            if (projectileMap.TryGetValue(id, out ProjectileComponent comp))
            {
                projectilePool.Return(comp.gameObject);
                projectileMap.Remove(id);
            }
        }
    }

    private void ProcessProjectileSpawns()
    {
        var spawned = projectileStore.SpawnedThisFrame;
        for (int i = 0; i < spawned.Count; i++)
        {
            ProjectileSimData projectile = spawned[i];
            GameObject go = projectilePool.Acquire(projectile.Position);
            if (go.TryGetComponent(out ProjectileComponent comp))
            {
                if (projectileMap.ContainsKey(projectile.Id))
                {
                    Debug.LogWarning($"PresentationAdapter: Duplicate projectile Id={projectile.Id}. Overwriting visual binding.");
                }

                comp.Initialize(projectile.Id);
                projectileMap[projectile.Id] = comp;
            }
            else
            {
                Debug.LogError($"PresentationAdapter: Projectile prefab is missing ProjectileComponent. Id={projectile.Id}");
                projectilePool.Return(go);
            }
        }
    }

    private void UpdateProjectilePositions()
    {
        var active = projectileStore.ActiveProjectiles;
        for (int i = 0; i < active.Count; i++)
        {
            ProjectileSimData projectile = active[i];
            if (projectileMap.TryGetValue(projectile.Id, out ProjectileComponent comp))
            {
                comp.transform.position = projectile.Position;
            }
        }
    }

    private void ReturnCreepsToSourcePools()
    {
        foreach (var kvp in creepVisuals)
        {
            kvp.Value.SourcePool.Return(kvp.Value.Component.gameObject);
        }

        creepVisuals.Clear();
    }

    private void ReturnTurretsToSourcePools()
    {
        foreach (var kvp in turretVisuals)
        {
            kvp.Value.SourcePool.Return(kvp.Value.Component.gameObject);
        }

        turretVisuals.Clear();
    }

    private void ReturnAllToPool<T>(Dictionary<int, T> map, GameObjectPool pool) where T : Component
    {
        foreach (var kvp in map)
        {
            pool.Return(kvp.Value.gameObject);
        }

        map.Clear();
    }
}
