using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private const int POOL_SIZE_MULTIPLIER = 10;

    [SerializeField] private HomeBaseComponent homeBase;
    [SerializeField] private SpawnPointComponent[] spawnPoints;
    [SerializeField] private GameObject creepPrefab;
    [SerializeField] private SpawnConfig spawnConfig;
    [SerializeField] private CreepDef creepDef;

    private GameStateMachine stateMachine;
    private SystemScheduler systemScheduler;
    private PresentationAdapter presentationAdapter;
    private GameSession gameSession;

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

        Vector3 basePosition = homeBase.transform.position;
        Vector3[] spawnPositions = ExtractSpawnPositions();

        gameSession = new GameSession();

        var spawnSystem = new SpawnSystem(
            gameSession.CreepStore,
            spawnPositions,
            basePosition,
            spawnConfig.SpawnInterval,
            spawnConfig.CreepsPerSpawn,
            creepDef.Speed);

        var movementSystem = new MovementSystem(gameSession.CreepStore);

        int poolSize = (spawnPositions.Length > 0 ? spawnPositions.Length : 1)
                       * spawnConfig.CreepsPerSpawn * POOL_SIZE_MULTIPLIER;
        var creepPool = new ObjectPooling.GameObjectPool(creepPrefab, poolSize, transform);

        presentationAdapter = new PresentationAdapter(gameSession.CreepStore, creepPool);
        systemScheduler = new SystemScheduler(new IGameSystem[] { spawnSystem, movementSystem });
        stateMachine = new GameStateMachine();

        var initState = new InitState(stateMachine.Fire, homeBase);
        var playingState = new PlayingState(stateMachine.Fire);

        stateMachine.AddState(GameState.Init, initState);
        stateMachine.AddState(GameState.Playing, playingState);

        stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        // Win/Lose transitions registered when those states are implemented (Stories 3, 9, 10)

        stateMachine.OnStateChanged += OnStateChanged;
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
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        Debug.Log($"State changed: {from} -> {to}");
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
