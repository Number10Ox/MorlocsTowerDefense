using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Composition root and game loop pump. Creates all systems, stores, pools, and presentation
// in Awake. Drives per-frame tick: input -> state -> systems -> visuals.
public class GameFlowController : MonoBehaviour
{
    private const int INITIAL_TURRET_POOL_SIZE = 20;
    private const int INITIAL_PROJECTILE_POOL_SIZE = 50;

    [SerializeField] private HomeBaseComponent homeBase;
    [SerializeField] private SpawnPointComponent[] spawnPoints;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private WaveConfig waveConfig;
    [SerializeField] private CreepDefinitions creepDefinitions;
    [SerializeField] private TurretDefinitions turretDefinitions;
    [SerializeField] private BaseConfig baseConfig;
    [SerializeField] private EconomyConfig economyConfig;
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private GameObject losePopupPrefab;
    [SerializeField] private GameObject winPopupPrefab;
    [SerializeField] private UIDocument hudDocument;

    private GameStateMachine stateMachine;
    private SystemScheduler systemScheduler;
    private PresentationAdapter presentationAdapter;
    private GameSession gameSession;
    private GameUiCoordinator uiCoordinator;
    private PlacementInput placementInput;
    private WaveSystem waveSystem;
    private SpawnSystem spawnSystem;
    private PlacementSystem placementSystem;
    private ProjectileSystem projectileSystem;
    private EconomySystem economySystem;

    private void Awake()
    {
        if (homeBase == null)
        {
            Debug.LogError("GameFlowController: HomeBaseComponent reference is not assigned.");
            enabled = false;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("GameFlowController: Projectile prefab reference is not assigned.");
            enabled = false;
            return;
        }

        if (waveConfig == null)
        {
            Debug.LogError("GameFlowController: WaveConfig reference is not assigned.");
            enabled = false;
            return;
        }

        if (creepDefinitions == null)
        {
            Debug.LogError("GameFlowController: CreepDefinitions reference is not assigned.");
            enabled = false;
            return;
        }

        if (turretDefinitions == null)
        {
            Debug.LogError("GameFlowController: TurretDefinitions reference is not assigned.");
            enabled = false;
            return;
        }

        if (baseConfig == null)
        {
            Debug.LogError("GameFlowController: BaseConfig reference is not assigned.");
            enabled = false;
            return;
        }

        if (economyConfig == null)
        {
            Debug.LogError("GameFlowController: EconomyConfig reference is not assigned.");
            enabled = false;
            return;
        }

        if (terrainLayerMask.value == 0)
        {
            Debug.LogWarning("GameFlowController: terrainLayerMask is set to Nothing. Turret placement raycasts will never hit.");
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("GameFlowController: No main camera found in scene.");
            enabled = false;
            return;
        }

        if (!TurretTypeDirectoryBuilder.TryBuild(
            turretDefinitions.Entries,
            out TurretTypeDirectory turretDirectory,
            out string turretBuildError))
        {
            Debug.LogError($"GameFlowController: {turretBuildError}");
            enabled = false;
            return;
        }

        if (!CreepTypeDirectoryBuilder.TryBuild(
            creepDefinitions.Entries,
            out CreepTypeDirectory creepDirectory,
            out string creepBuildError))
        {
            Debug.LogError($"GameFlowController: {creepBuildError}");
            enabled = false;
            return;
        }

        if (!ValidateWaveCreepTypes(waveConfig.Waves, creepDirectory.StatsByType))
        {
            enabled = false;
            return;
        }

        Vector3 basePosition = homeBase.transform.position;
        Vector3[] spawnPositions = ExtractSpawnPositions();

        TurretType defaultType = turretDirectory.DefaultType;
        gameSession = new GameSession(baseConfig.MaxHealth, economyConfig.StartingCoins, defaultType);

        // Phase 1 — World Update
        waveSystem = new WaveSystem(
            gameSession.WaveStore,
            gameSession.CreepStore,
            waveConfig.Waves);

        spawnSystem = new SpawnSystem(
            gameSession.CreepStore,
            gameSession.WaveStore,
            spawnPositions,
            basePosition,
            creepDirectory.StatsByType);

        var movementSystem = new MovementSystem(gameSession.CreepStore);

        placementInput = new PlacementInput();
        placementSystem = new PlacementSystem(
            gameSession.TurretStore,
            placementInput,
            gameSession.EconomyStore,
            gameSession.TurretSelectionStore,
            turretDirectory.StatsByType);

        // Phase 2 — Combat
        projectileSystem = new ProjectileSystem(
            gameSession.TurretStore,
            gameSession.CreepStore,
            gameSession.ProjectileStore);

        var damageSystem = new DamageSystem(
            gameSession.CreepStore,
            gameSession.BaseStore,
            gameSession.ProjectileStore);

        // Phase 3 — Resolution
        economySystem = new EconomySystem(
            gameSession.EconomyStore,
            gameSession.TurretStore,
            turretDirectory.StatsByType);

        damageSystem.OnCreepKilled += economySystem.HandleCreepKilled;

        int maxCreepsInWave = MaxCreepsInAnyWave(waveConfig.Waves);
        int totalCreepPoolSize = Mathf.Max(maxCreepsInWave, 1) * 2;
        int creepTypeCount = Mathf.Max(1, creepDirectory.OrderedTypes.Length);
        int perTypeCreepPoolSize = Mathf.CeilToInt((float)totalCreepPoolSize / creepTypeCount);

        var creepPoolByType = new Dictionary<CreepType, ObjectPooling.GameObjectPool>(creepDirectory.PrefabsByType.Count);
        foreach (var kvp in creepDirectory.PrefabsByType)
        {
            creepPoolByType[kvp.Key] =
                new ObjectPooling.GameObjectPool(kvp.Value, perTypeCreepPoolSize, transform);
        }

        var turretPoolByType = new Dictionary<TurretType, ObjectPooling.GameObjectPool>(turretDirectory.PrefabsByType.Count);
        foreach (var kvp in turretDirectory.PrefabsByType)
        {
            turretPoolByType[kvp.Key] =
                new ObjectPooling.GameObjectPool(kvp.Value, INITIAL_TURRET_POOL_SIZE, transform);
        }

        var projectilePool = new ObjectPooling.GameObjectPool(projectilePrefab, INITIAL_PROJECTILE_POOL_SIZE, transform);

        presentationAdapter = new PresentationAdapter(
            new PresentationAdapter.StoreDeps(
                gameSession.CreepStore,
                gameSession.TurretStore,
                gameSession.ProjectileStore),
            new PresentationAdapter.PoolDeps(
                creepPoolByType,
                turretPoolByType,
                projectilePool),
            new PresentationAdapter.InputDeps(
                placementInput,
                gameSession.TurretSelectionStore,
                turretDirectory.OrderedTypes),
            new PresentationAdapter.SceneDeps(
                mainCamera,
                terrainLayerMask));

        systemScheduler = new SystemScheduler(new IGameSystem[]
        {
            // Phase 1 — World Update
            waveSystem, spawnSystem, movementSystem, placementSystem,
            // Phase 2 — Combat
            projectileSystem, damageSystem,
            // Phase 3 — Resolution
            economySystem
        });

        stateMachine = new GameStateMachine();

        var initState = new InitState(stateMachine.Fire, homeBase);
        var playingState = new PlayingState(stateMachine.Fire, gameSession.BaseStore, gameSession.WaveStore);
        var loseState = new LoseState(stateMachine.Fire);
        var winState = new WinState(stateMachine.Fire);

        stateMachine.AddState(GameState.Init, initState);
        stateMachine.AddState(GameState.Playing, playingState);
        stateMachine.AddState(GameState.Lose, loseState);
        stateMachine.AddState(GameState.Win, winState);

        stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        stateMachine.AddTransition(GameState.Playing, GameTrigger.BaseDestroyed, GameState.Lose);
        stateMachine.AddTransition(GameState.Playing, GameTrigger.AllWavesCleared, GameState.Win);
        stateMachine.AddTransition(GameState.Win, GameTrigger.RestartRequested, GameState.Init);
        stateMachine.AddTransition(GameState.Lose, GameTrigger.RestartRequested, GameState.Init);

        stateMachine.OnStateChanged += OnStateChanged;

        BaseHealthHud baseHealthHud = null;
        CoinHud coinHud = null;
        TurretSelectionHud turretSelectionHud = null;
        RestartHintHud restartHintHud = null;
        if (hudDocument != null)
        {
            baseHealthHud = new BaseHealthHud(hudDocument);
            coinHud = new CoinHud(hudDocument);
            turretSelectionHud = new TurretSelectionHud(hudDocument, turretDirectory.OrderedStats, defaultType);
            restartHintHud = new RestartHintHud(hudDocument);
        }

        uiCoordinator = new GameUiCoordinator(
            stateMachine,
            new GameUiCoordinator.StoreDeps(
                gameSession.BaseStore,
                gameSession.EconomyStore,
                gameSession.TurretSelectionStore),
            new GameUiCoordinator.HudDeps(
                baseHealthHud,
                coinHud,
                turretSelectionHud,
                restartHintHud),
            new GameUiCoordinator.PopupDeps(
                losePopupPrefab,
                winPopupPrefab,
                transform));
    }

    private void Start()
    {
        if (stateMachine == null) return;
        stateMachine.Start(GameState.Init);
    }

    private void Update()
    {
        presentationAdapter.CollectInput();

        if ((stateMachine.CurrentStateId == GameState.Win
            || stateMachine.CurrentStateId == GameState.Lose)
            && placementInput.RestartRequested)
        {
            stateMachine.Fire(GameTrigger.RestartRequested);
        }

        // Trigger resolution happens inside Tick. If a transition into Playing
        // occurs this frame, systems won't tick until the next frame.
        stateMachine.Tick(Time.deltaTime);

        // Housekeeping runs every frame: clears per-frame change lists and flushes
        // deferred removals so SyncVisuals never reprocesses stale data.
        gameSession.BeginFrame();

        if (stateMachine.CurrentStateId == GameState.Playing)
        {
            systemScheduler.Tick(Time.deltaTime);
        }

        presentationAdapter.SyncVisuals();
    }

    private void OnDestroy()
    {
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= OnStateChanged;
        }

        uiCoordinator?.Teardown();
        uiCoordinator = null;
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        if (to != GameState.Init) return;
        if (from != GameState.Win && from != GameState.Lose) return;

        presentationAdapter.ResetVisuals();
        gameSession.Reset();

        waveSystem.Reset();
        spawnSystem.Reset();
        placementSystem.Reset();
        projectileSystem.Reset();
        economySystem.Reset();

        uiCoordinator.Refresh();
    }

    private static int MaxCreepsInAnyWave(WaveDefinition[] waves)
    {
        int max = 0;
        for (int w = 0; w < waves.Length; w++)
        {
            int waveTotal = 0;
            var entries = waves[w].Entries;
            if (entries != null)
            {
                for (int e = 0; e < entries.Length; e++)
                {
                    waveTotal += entries[e].Count;
                }
            }
            if (waveTotal > max) max = waveTotal;
        }
        return max;
    }

    private static bool ValidateWaveCreepTypes(
        WaveDefinition[] waves,
        IReadOnlyDictionary<CreepType, CreepTypeStats> statsByType)
    {
        for (int w = 0; w < waves.Length; w++)
        {
            var entries = waves[w].Entries;
            if (entries == null) continue;
            for (int e = 0; e < entries.Length; e++)
            {
                CreepType type = entries[e].CreepType;
                if (!statsByType.ContainsKey(type))
                {
                    Debug.LogError(
                        $"GameFlowController: Wave {w} entry {e} references CreepType {type} " +
                        "which has no stats in CreepDefinitions.");
                    return false;
                }
            }
        }
        return true;
    }

    private Vector3[] ExtractSpawnPositions()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("GameFlowController: No SpawnPoints assigned. No creeps will spawn.");
            return new Vector3[0];
        }

        int validCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) validCount++;
            else Debug.LogWarning($"GameFlowController: SpawnPoint at index {i} is null. Skipping.");
        }

        Vector3[] positions = new Vector3[validCount];
        int index = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                positions[index++] = spawnPoints[i].transform.position;
            }
        }
        return positions;
    }
}
