using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameStateMachineTests
{
    private GameStateMachine sm;

    private class MockState : IGameState
    {
        public int EnterCount;
        public int TickCount;
        public int ExitCount;
        public float LastDeltaTime;
        public Action OnEnterCallback;
        public Action OnExitCallback;

        public void Enter()
        {
            EnterCount++;
            OnEnterCallback?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            TickCount++;
            LastDeltaTime = deltaTime;
        }

        public void Exit()
        {
            ExitCount++;
            OnExitCallback?.Invoke();
        }
    }

    [SetUp]
    public void SetUp()
    {
        sm = new GameStateMachine();
    }

    // --- Start ---

    [Test]
    public void Start_SetsCurrentStateId()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);

        sm.Start(GameState.Init);

        Assert.AreEqual(GameState.Init, sm.CurrentStateId);
    }

    [Test]
    public void Start_CallsEnterOnInitialState()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);

        sm.Start(GameState.Init);

        Assert.AreEqual(1, initState.EnterCount);
    }

    [Test]
    public void Start_WithUnregisteredState_LogsError()
    {
        LogAssert.Expect(LogType.Error, new Regex(@"No state registered for"));
        sm.Start(GameState.Init);
    }

    [Test]
    public void Start_CalledTwice_LogsWarningAndIgnores()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.Start(GameState.Init);

        LogAssert.Expect(LogType.Warning, new Regex(@"Start\(\) called more than once"));
        sm.Start(GameState.Init);

        Assert.AreEqual(1, initState.EnterCount);
    }

    // --- Fire ---

    [Test]
    public void Fire_BeforeStart_LogsWarningAndIgnores()
    {
        LogAssert.Expect(LogType.Warning, new Regex(@"Fire\(\) called before Start\(\)"));
        sm.Fire(GameTrigger.SceneValidated);
    }

    [Test]
    public void Fire_InvalidTrigger_LogsWarningAndStaysInCurrentState()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.Start(GameState.Init);

        sm.Fire(GameTrigger.BaseDestroyed);
        LogAssert.Expect(LogType.Warning, new Regex(@"No transition for"));
        sm.Tick(0.016f);

        Assert.AreEqual(GameState.Init, sm.CurrentStateId);
    }

    [Test]
    public void Fire_CalledTwiceBeforeResolve_OverwritesWithWarning()
    {
        var initState = new MockState();
        var playingState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        sm.Fire(GameTrigger.BaseDestroyed);
        LogAssert.Expect(LogType.Warning, new Regex(@"overwritten by"));
        sm.Fire(GameTrigger.SceneValidated);

        sm.Tick(0.016f);

        Assert.AreEqual(GameState.Playing, sm.CurrentStateId);
        Assert.AreEqual(1, initState.ExitCount);
        Assert.AreEqual(1, playingState.EnterCount);
    }

    // --- Tick ---

    [Test]
    public void Tick_BeforeStart_LogsWarningAndIgnores()
    {
        LogAssert.Expect(LogType.Warning, new Regex(@"Tick\(\) called before Start\(\)"));
        sm.Tick(0.016f);
    }

    [Test]
    public void Tick_WithPendingTrigger_TransitionsToDestination()
    {
        var initState = new MockState();
        var playingState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        sm.Fire(GameTrigger.SceneValidated);
        sm.Tick(0.016f);

        Assert.AreEqual(GameState.Playing, sm.CurrentStateId);
    }

    [Test]
    public void Tick_WithPendingTrigger_CallsExitOnOldAndEnterOnNew()
    {
        var initState = new MockState();
        var playingState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        sm.Fire(GameTrigger.SceneValidated);
        sm.Tick(0.016f);

        Assert.AreEqual(1, initState.ExitCount);
        Assert.AreEqual(1, playingState.EnterCount);
    }

    [Test]
    public void Tick_NoPendingTrigger_TicksCurrentState()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.Start(GameState.Init);

        sm.Tick(0.016f);

        Assert.AreEqual(1, initState.TickCount);
    }

    [Test]
    public void Tick_AfterTransition_TicksNewState()
    {
        var initState = new MockState();
        var playingState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        sm.Fire(GameTrigger.SceneValidated);
        sm.Tick(0.016f);

        Assert.AreEqual(1, playingState.TickCount);
        Assert.AreEqual(0, initState.TickCount);
    }

    [Test]
    public void Tick_ForwardsDeltaTimeToCurrentState()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.Start(GameState.Init);

        sm.Tick(0.033f);

        Assert.AreEqual(0.033f, initState.LastDeltaTime, 0.0001f);
    }

    // --- Transitions ---

    [Test]
    public void Transition_ToUnregisteredDestination_LogsError()
    {
        var initState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        sm.Fire(GameTrigger.SceneValidated);
        LogAssert.Expect(LogType.Error, new Regex(@"no state is registered"));
        sm.Tick(0.016f);

        Assert.AreEqual(GameState.Init, sm.CurrentStateId);
        Assert.AreEqual(0, initState.ExitCount);
    }

    [Test]
    public void AddTransition_DuplicateKey_LogsWarningAndOverwrites()
    {
        var initState = new MockState();
        var playingState = new MockState();
        var loseState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddState(GameState.Lose, loseState);

        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        LogAssert.Expect(LogType.Warning, new Regex(@"Duplicate transition.*already registered"));
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Lose);

        sm.Start(GameState.Init);
        sm.Fire(GameTrigger.SceneValidated);
        sm.Tick(0.016f);

        Assert.AreEqual(GameState.Lose, sm.CurrentStateId);
    }

    // --- OnStateChanged ---

    [Test]
    public void OnStateChanged_PassesCorrectFromAndTo()
    {
        var initState = new MockState();
        var playingState = new MockState();
        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        GameState capturedFrom = default;
        GameState capturedTo = default;
        sm.OnStateChanged += (from, to) =>
        {
            capturedFrom = from;
            capturedTo = to;
        };

        sm.Fire(GameTrigger.SceneValidated);
        sm.Tick(0.016f);

        Assert.AreEqual(GameState.Init, capturedFrom);
        Assert.AreEqual(GameState.Playing, capturedTo);
    }

    [Test]
    public void OnStateChanged_FiresAfterExitAndEnter()
    {
        var callOrder = new List<string>();
        var initState = new MockState();
        initState.OnExitCallback = () => callOrder.Add("Exit");
        var playingState = new MockState();
        playingState.OnEnterCallback = () => callOrder.Add("Enter");

        sm.AddState(GameState.Init, initState);
        sm.AddState(GameState.Playing, playingState);
        sm.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        sm.Start(GameState.Init);

        sm.OnStateChanged += (from, to) => callOrder.Add("Event");

        sm.Fire(GameTrigger.SceneValidated);
        sm.Tick(0.016f);

        Assert.AreEqual(new List<string> { "Exit", "Enter", "Event" }, callOrder);
    }

    // --- Integration ---

    [Test]
    public void FullFlow_InitToPlaying_TransitionsOnFirstTick()
    {
        var homeBase = new GameObject("Base").AddComponent<HomeBaseComponent>();
        try
        {
            var stateMachine = new GameStateMachine();
            var initState = new InitState(stateMachine.Fire, homeBase);
            var playingState = new PlayingState(stateMachine.Fire, new BaseStore(100));

            stateMachine.AddState(GameState.Init, initState);
            stateMachine.AddState(GameState.Playing, playingState);
            stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);

            stateMachine.Start(GameState.Init);
            Assert.AreEqual(GameState.Init, stateMachine.CurrentStateId);

            stateMachine.Tick(0.016f);
            Assert.AreEqual(GameState.Playing, stateMachine.CurrentStateId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(homeBase.gameObject);
        }
    }
}
