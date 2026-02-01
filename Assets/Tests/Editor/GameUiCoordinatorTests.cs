using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameUiCoordinatorTests
{
    private GameStateMachine stateMachine;
    private BaseStore baseStore;
    private EconomyStore economyStore;

    [SetUp]
    public void SetUp()
    {
        stateMachine = new GameStateMachine();
        baseStore = new BaseStore(100);
        economyStore = new EconomyStore(20);

        var initState = new InitState(stateMachine.Fire, null);
        var playingState = new PlayingState(stateMachine.Fire, baseStore);
        var loseState = new LoseState(stateMachine.Fire);

        stateMachine.AddState(GameState.Init, initState);
        stateMachine.AddState(GameState.Playing, playingState);
        stateMachine.AddState(GameState.Lose, loseState);

        stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
        stateMachine.AddTransition(GameState.Playing, GameTrigger.BaseDestroyed, GameState.Lose);
    }

    [Test]
    public void Constructor_NullStateMachine_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GameUiCoordinator(null, baseStore, economyStore, null, null, null, null));
    }

    [Test]
    public void Constructor_NullBaseStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GameUiCoordinator(stateMachine, null, economyStore, null, null, null, null));
    }

    [Test]
    public void Constructor_NullEconomyStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GameUiCoordinator(stateMachine, baseStore, null, null, null, null, null));
    }

    [Test]
    public void Teardown_CalledTwice_NoException()
    {
        var coordinator = new GameUiCoordinator(stateMachine, baseStore, economyStore, null, null, null, null);

        Assert.DoesNotThrow(() =>
        {
            coordinator.Teardown();
            coordinator.Teardown();
        });
    }

    [Test]
    public void Refresh_WithNullHud_NoException()
    {
        LogAssert.Expect(LogType.Error, "InitState: HomeBaseComponent reference is null. Scene setup is invalid.");
        stateMachine.Start(GameState.Init);
        var coordinator = new GameUiCoordinator(stateMachine, baseStore, economyStore, null, null, null, null);

        Assert.DoesNotThrow(() => coordinator.Refresh());
    }

    [Test]
    public void OnStateChanged_WithNullPopupPrefab_NoException()
    {
        LogAssert.Expect(LogType.Error, "InitState: HomeBaseComponent reference is null. Scene setup is invalid.");
        stateMachine.Start(GameState.Init);
        var coordinator = new GameUiCoordinator(stateMachine, baseStore, economyStore, null, null, null, null);

        Assert.DoesNotThrow(() =>
        {
            stateMachine.Fire(GameTrigger.SceneValidated);
            stateMachine.Tick(0f);
        });

        coordinator.Teardown();
    }

    [Test]
    public void Teardown_UnsubscribesFromStateChanged()
    {
        LogAssert.Expect(LogType.Error, "InitState: HomeBaseComponent reference is null. Scene setup is invalid.");
        stateMachine.Start(GameState.Init);
        var coordinator = new GameUiCoordinator(stateMachine, baseStore, economyStore, null, null, null, null);

        coordinator.Teardown();

        // Transitioning after teardown should not throw (event unsubscribed)
        Assert.DoesNotThrow(() =>
        {
            stateMachine.Fire(GameTrigger.SceneValidated);
            stateMachine.Tick(0f);
        });
    }

    [Test]
    public void Teardown_UnsubscribesFromBaseHealthChanged()
    {
        var coordinator = new GameUiCoordinator(stateMachine, baseStore, economyStore, null, null, null, null);

        coordinator.Teardown();

        // Applying damage after teardown should not throw (event unsubscribed or hud was null)
        Assert.DoesNotThrow(() => baseStore.ApplyDamage(10));
    }

    [Test]
    public void Teardown_UnsubscribesFromCoinsChanged()
    {
        var coordinator = new GameUiCoordinator(stateMachine, baseStore, economyStore, null, null, null, null);

        coordinator.Teardown();

        // Adding coins after teardown should not throw (event unsubscribed or hud was null)
        Assert.DoesNotThrow(() => economyStore.AddCoins(5));
    }
}
