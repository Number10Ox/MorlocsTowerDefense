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
    [SerializeField] private GameObject turretPrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private SpawnConfig spawnConfig;
    [SerializeField] private CreepDef creepDef;
    [SerializeField] private TurretDef turretDef;
    [SerializeField] private BaseConfig baseConfig;
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

        if (turretPrefab == null)
        {
            Debug.LogError("GameFlowController: Turret prefab reference is not assigned.");
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

        if (turretDef == null)
        {
            Debug.LogError("GameFlowController: TurretDef reference is not assigned.");
            enabled = false;
            return;
        }

        if (baseConfig == null)
        {
            Debug.LogError("GameFlowController: BaseConfig reference is not assigned.");
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

        Vector3 basePosition = homeBase.transform.position;
        Vector3[] spawnPositions = ExtractSpawnPositions();

        gameSession = new GameSession(baseConfig.MaxHealth);

        var spawnSystem = new SpawnSystem(
            gameSession.CreepStore,
            spawnPositions,
            basePosition,
            spawnConfig.SpawnInterval,
            spawnConfig.CreepsPerSpawn,
            creepDef.Speed,
            creepDef.DamageToBase,
            creepDef.MaxHealth);

        var movementSystem = new MovementSystem(gameSession.CreepStore);

        var placementInput = new PlacementInput();
        var placementSystem = new PlacementSystem(
            gameSession.TurretStore,
            placementInput,
            turretDef.Range,
            turretDef.FireInterval,
            turretDef.Damage,
            turretDef.ProjectileSpeed);

        var projectileSystem = new ProjectileSystem(
            gameSession.TurretStore,
            gameSession.CreepStore,
            gameSession.ProjectileStore);

        var damageSystem = new DamageSystem(
            gameSession.CreepStore,
            gameSession.BaseStore,
            gameSession.ProjectileStore);

        int creepPoolSize = (spawnPositions.Length > 0 ? spawnPositions.Length : 1)
                            * spawnConfig.CreepsPerSpawn * POOL_SIZE_MULTIPLIER;
        var creepPool = new ObjectPooling.GameObjectPool(creepPrefab, creepPoolSize, transform);
        var turretPool = new ObjectPooling.GameObjectPool(turretPrefab, INITIAL_TURRET_POOL_SIZE, transform);
        var projectilePool = new ObjectPooling.GameObjectPool(projectilePrefab, INITIAL_PROJECTILE_POOL_SIZE, transform);

        presentationAdapter = new PresentationAdapter(
            gameSession.CreepStore,
            creepPool,
            gameSession.TurretStore,
            turretPool,
            gameSession.ProjectileStore,
            projectilePool,
            placementInput,
            mainCamera,
            terrainLayerMask);

        systemScheduler = new SystemScheduler(new IGameSystem[]
        {
            spawnSystem, movementSystem, placementSystem, projectileSystem, damageSystem
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
        if (hudDocument != null)
        {
            baseHealthHud = new BaseHealthHud(hudDocument);
        }

        uiCoordinator = new GameUiCoordinator(
            stateMachine,
            gameSession.BaseStore,
            baseHealthHud,
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
