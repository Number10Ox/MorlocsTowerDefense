using UnityEngine;
using UnityEngine.UIElements;

public class GameBootstrap : MonoBehaviour
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
    private BaseHealthHud baseHealthHud;
    private GameObject losePopupInstance;

    private void Awake()
    {
        if (homeBase == null)
        {
            Debug.LogError("GameBootstrap: HomeBaseComponent reference is not assigned.");
            enabled = false;
            return;
        }

        if (creepPrefab == null)
        {
            Debug.LogError("GameBootstrap: Creep prefab reference is not assigned.");
            enabled = false;
            return;
        }

        if (turretPrefab == null)
        {
            Debug.LogError("GameBootstrap: Turret prefab reference is not assigned.");
            enabled = false;
            return;
        }

        if (projectilePrefab == null)
        {
            Debug.LogError("GameBootstrap: Projectile prefab reference is not assigned.");
            enabled = false;
            return;
        }

        if (spawnConfig == null)
        {
            Debug.LogError("GameBootstrap: SpawnConfig reference is not assigned.");
            enabled = false;
            return;
        }

        if (creepDef == null)
        {
            Debug.LogError("GameBootstrap: CreepDef reference is not assigned.");
            enabled = false;
            return;
        }

        if (turretDef == null)
        {
            Debug.LogError("GameBootstrap: TurretDef reference is not assigned.");
            enabled = false;
            return;
        }

        if (baseConfig == null)
        {
            Debug.LogError("GameBootstrap: BaseConfig reference is not assigned.");
            enabled = false;
            return;
        }

        if (terrainLayerMask.value == 0)
        {
            Debug.LogWarning("GameBootstrap: terrainLayerMask is set to Nothing. Turret placement raycasts will never hit.");
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("GameBootstrap: No main camera found in scene.");
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

        stateMachine.OnStateChanged += OnStateChanged;

        if (hudDocument != null)
        {
            baseHealthHud = new BaseHealthHud(hudDocument);
            baseHealthHud.UpdateHealth(baseConfig.MaxHealth, baseConfig.MaxHealth);
            gameSession.BaseStore.OnBaseHealthChanged += OnBaseHealthChanged;
        }
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
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= OnStateChanged;
        }

        if (gameSession != null)
        {
            gameSession.BaseStore.OnBaseHealthChanged -= OnBaseHealthChanged;
        }

        if (losePopupInstance != null)
        {
            Destroy(losePopupInstance);
            losePopupInstance = null;
        }
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        Debug.Log($"State changed: {from} -> {to}");

        if (to == GameState.Lose && losePopupPrefab != null)
        {
            losePopupInstance = Instantiate(losePopupPrefab);
        }

        if (from == GameState.Lose && losePopupInstance != null)
        {
            Destroy(losePopupInstance);
            losePopupInstance = null;
        }

        if (baseHealthHud != null)
        {
            baseHealthHud.SetVisible(to == GameState.Playing);
        }
    }

    private void OnBaseHealthChanged(int current, int max)
    {
        if (baseHealthHud != null)
        {
            baseHealthHud.UpdateHealth(current, max);
        }
    }

    private Vector3[] ExtractSpawnPositions()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("GameBootstrap: No SpawnPoints assigned. No creeps will spawn.");
            return new Vector3[0];
        }

        int validCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null) validCount++;
            else Debug.LogWarning($"GameBootstrap: SpawnPoint at index {i} is null. Skipping.");
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
