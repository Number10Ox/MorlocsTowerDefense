using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Composition root and game loop pump. Creates all systems, stores, pools, and presentation
// in Awake. Drives per-frame tick: input -> state -> systems -> visuals.
public class GameFlowController : MonoBehaviour
{
    private const int POOL_SIZE_MULTIPLIER = 10;
    private const int INITIAL_TURRET_POOL_SIZE = 20;
    private const int INITIAL_PROJECTILE_POOL_SIZE = 50;

    [SerializeField] private HomeBaseComponent homeBase;
    [SerializeField] private SpawnPointComponent[] spawnPoints;
    [SerializeField] private GameObject creepPrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private SpawnConfig spawnConfig;
    [SerializeField] private CreepDef creepDef;
    [SerializeField] private TurretDefinitions turretDefinitions;
    [SerializeField] private BaseConfig baseConfig;
    [SerializeField] private EconomyConfig economyConfig;
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private GameObject losePopupPrefab;
    [SerializeField] private UIDocument hudDocument;

    private GameStateMachine stateMachine;
    private SystemScheduler systemScheduler;
    private PresentationAdapter presentationAdapter;
    private GameSession gameSession;
    private GameUiCoordinator uiCoordinator;

    private void Awake()
    {
        if (homeBase == null)
        {
            Debug.LogError("GameFlowController: HomeBaseComponent reference is not assigned.");
            enabled = false;
            return;
        }

        if (creepPrefab == null)
        {
            Debug.LogError("GameFlowController: Creep prefab reference is not assigned.");
            enabled = false;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("GameFlowController: Projectile prefab reference is not assigned.");
            enabled = false;
            return;
        }

        if (spawnConfig == null)
        {
            Debug.LogError("GameFlowController: SpawnConfig reference is not assigned.");
            enabled = false;
            return;
        }

        if (creepDef == null)
        {
            Debug.LogError("GameFlowController: CreepDef reference is not assigned.");
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
            out string catalogError))
        {
            Debug.LogError($"GameFlowController: {catalogError}");
            enabled = false;
            return;
        }

        Vector3 basePosition = homeBase.transform.position;
        Vector3[] spawnPositions = ExtractSpawnPositions();

        TurretType defaultType = turretDirectory.DefaultType;
        gameSession = new GameSession(baseConfig.MaxHealth, economyConfig.StartingCoins, defaultType);

        // Phase 1 — World Update
        var spawnSystem = new SpawnSystem(
            gameSession.CreepStore,
            spawnPositions,
            basePosition,
            spawnConfig.SpawnInterval,
            spawnConfig.CreepsPerSpawn,
            creepDef.Speed,
            creepDef.DamageToBase,
            creepDef.MaxHealth,
            creepDef.CoinReward);

        var movementSystem = new MovementSystem(gameSession.CreepStore);

        var placementInput = new PlacementInput();
        var placementSystem = new PlacementSystem(
            gameSession.TurretStore,
            placementInput,
            gameSession.EconomyStore,
            gameSession.TurretSelectionStore,
            turretDirectory.StatsByType);

        // Phase 2 — Combat
        var projectileSystem = new ProjectileSystem(
            gameSession.TurretStore,
            gameSession.CreepStore,
            gameSession.ProjectileStore);

        var damageSystem = new DamageSystem(
            gameSession.CreepStore,
            gameSession.BaseStore,
            gameSession.ProjectileStore);

        // Phase 3 — Resolution
        var economySystem = new EconomySystem(
            gameSession.EconomyStore,
            gameSession.TurretStore,
            turretDirectory.StatsByType);

        damageSystem.OnCreepKilled += economySystem.HandleCreepKilled;

        int creepPoolSize = (spawnPositions.Length > 0 ? spawnPositions.Length : 1)
                            * spawnConfig.CreepsPerSpawn * POOL_SIZE_MULTIPLIER;
        var creepPool = new ObjectPooling.GameObjectPool(creepPrefab, creepPoolSize, transform);

        var turretPoolByType = new Dictionary<TurretType, ObjectPooling.GameObjectPool>(turretDirectory.PrefabsByType.Count);
        foreach (var kvp in turretDirectory.PrefabsByType)
        {
            turretPoolByType[kvp.Key] =
                new ObjectPooling.GameObjectPool(kvp.Value, INITIAL_TURRET_POOL_SIZE, transform);
        }

        var projectilePool = new ObjectPooling.GameObjectPool(projectilePrefab, INITIAL_PROJECTILE_POOL_SIZE, transform);

        presentationAdapter = new PresentationAdapter(
            gameSession.CreepStore,
            creepPool,
            gameSession.TurretStore,
            turretPoolByType,
            gameSession.ProjectileStore,
            projectilePool,
            placementInput,
            gameSession.TurretSelectionStore,
            turretDirectory.OrderedTypes,
            mainCamera,
            terrainLayerMask);

        systemScheduler = new SystemScheduler(new IGameSystem[]
        {
            // Phase 1 — World Update
            spawnSystem, movementSystem, placementSystem,
            // Phase 2 — Combat
            projectileSystem, damageSystem,
            // Phase 3 — Resolution
            economySystem
        });

        stateMachine = new GameStateMachine();

        var initState = new InitState(stateMachine.Fire, homeBase);
        var playingState = new PlayingState(stateMachine.Fire, gameSession.BaseStore);
        var loseState = new LoseState(stateMachine.Fire);

        stateMachine.AddState(GameState.Init, initState);
        stateMachine.AddState(GameState.Playing, playingState);
        stateMachine.AddState(GameState.Lose, loseState);

        stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        stateMachine.AddTransition(GameState.Playing, GameTrigger.BaseDestroyed, GameState.Lose);
        // Win transition registered when WinState is implemented (Story 9)
        // Restart transitions registered when RestartState is implemented (Story 10)

        BaseHealthHud baseHealthHud = null;
        CoinHud coinHud = null;
        TurretSelectionHud turretSelectionHud = null;
        if (hudDocument != null)
        {
            baseHealthHud = new BaseHealthHud(hudDocument);
            coinHud = new CoinHud(hudDocument);
            turretSelectionHud = new TurretSelectionHud(hudDocument, turretDirectory.OrderedStats, defaultType);
        }

        uiCoordinator = new GameUiCoordinator(
            stateMachine,
            gameSession.BaseStore,
            gameSession.EconomyStore,
            gameSession.TurretSelectionStore,
            baseHealthHud,
            coinHud,
            turretSelectionHud,
            losePopupPrefab,
            transform);
    }

    private void Start()
    {
        if (stateMachine == null) return;
        stateMachine.Start(GameState.Init);
    }

    private void Update()
    {
        presentationAdapter.CollectInput();

        // Trigger resolution happens inside Tick. If a transition into Playing
        // occurs this frame, systems won't tick until the next frame.
        stateMachine.Tick(Time.deltaTime);

        if (stateMachine.CurrentStateId == GameState.Playing)
        {
            gameSession.BeginFrame();
            systemScheduler.Tick(Time.deltaTime);
        }

        presentationAdapter.SyncVisuals();
    }

    private void OnDestroy()
    {
        uiCoordinator?.Teardown();
        uiCoordinator = null;
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
