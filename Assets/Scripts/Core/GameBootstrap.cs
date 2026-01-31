using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private HomeBaseComponent homeBase;

    private GameStateMachine stateMachine;
    private SystemScheduler systemScheduler;
    private PresentationAdapter presentationAdapter;

    private void Awake()
    {
        if (homeBase == null)
        {
            Debug.LogError("GameBootstrap: HomeBaseComponent reference is not assigned.");
            enabled = false;
            return;
        }

        presentationAdapter = new PresentationAdapter();
        systemScheduler = new SystemScheduler(new IGameSystem[0]);
        stateMachine = new GameStateMachine();

        var initState = new InitState(stateMachine.Fire, homeBase);
        var playingState = new PlayingState(stateMachine.Fire);

        stateMachine.AddState(GameState.Init, initState);
        stateMachine.AddState(GameState.Playing, playingState);

        stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        stateMachine.AddTransition(GameState.Playing, GameTrigger.BaseDestroyed, GameState.Lose);
        stateMachine.AddTransition(GameState.Playing, GameTrigger.AllWavesCleared, GameState.Win);
        stateMachine.AddTransition(GameState.Win, GameTrigger.RestartRequested, GameState.Init);
        stateMachine.AddTransition(GameState.Lose, GameTrigger.RestartRequested, GameState.Init);

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
        stateMachine.Tick(Time.deltaTime);

        if (stateMachine.CurrentStateId == GameState.Playing)
        {
            systemScheduler.Tick(Time.deltaTime);
        }

        presentationAdapter.SyncVisuals();
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        Debug.Log($"State changed: {from} -> {to}");
    }
}
