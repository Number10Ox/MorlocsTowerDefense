using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameResetIntegrationTests
{
    private const int BASE_MAX_HEALTH = 100;
    private const int STARTING_COINS = 50;
    private const float DT = 0.016f;

    private static readonly Vector3 BASE_POSITION = new Vector3(10f, 0f, 0f);
    private static readonly Vector3[] SPAWN_POSITIONS = new[] { Vector3.zero };

    private static readonly Dictionary<CreepType, CreepTypeStats> CREEP_STATS = new()
    {
        { CreepType.Small, new CreepTypeStats(CreepType.Small, 2f, 1, 10, 5) }
    };

    private static readonly Dictionary<TurretType, TurretTypeStats> TURRET_STATS = new()
    {
        { TurretType.Regular, new TurretTypeStats(TurretType.Regular, 5f, 1f, 10, 10f, 10) }
    };

    private static readonly WaveEntryDefinition[] WAVE_ENTRIES = new[]
    {
        new WaveEntryDefinition(CreepType.Small, 2, 0.01f)
    };

    private static readonly WaveDefinition[] WAVES = new[]
    {
        new WaveDefinition(WAVE_ENTRIES, 0f)
    };

    [Test]
    public void FullPopulateAndReset_AllStateClean()
    {
        var session = new GameSession(BASE_MAX_HEALTH, STARTING_COINS, TurretType.Regular);

        var waveSystem = new WaveSystem(session.WaveStore, session.CreepStore, WAVES);
        var spawnSystem = new SpawnSystem(session.CreepStore, session.WaveStore, SPAWN_POSITIONS, BASE_POSITION, CREEP_STATS);
        var placementInput = new PlacementInput();
        var placementSystem = new PlacementSystem(session.TurretStore, placementInput, session.EconomyStore, session.TurretSelectionStore, TURRET_STATS);
        var projectileSystem = new ProjectileSystem(session.TurretStore, session.CreepStore, session.ProjectileStore);
        var economySystem = new EconomySystem(session.EconomyStore, session.TurretStore, TURRET_STATS);

        // Simulate: spawn creeps, place a turret, tick systems
        session.BeginFrame();
        waveSystem.Tick(DT);
        spawnSystem.Tick(DT);
        Assert.IsTrue(session.CreepStore.ActiveCreeps.Count > 0);

        placementInput.PlaceRequested = true;
        placementInput.WorldPosition = new Vector3(5f, 0f, 0f);
        placementSystem.Tick(DT);
        Assert.AreEqual(1, session.TurretStore.ActiveTurrets.Count);

        session.BeginFrame();
        projectileSystem.Tick(DT);
        economySystem.Tick(DT);

        // Reset everything (same order as GameFlowController.OnStateChanged)
        session.Reset();
        waveSystem.Reset();
        spawnSystem.Reset();
        placementSystem.Reset();
        projectileSystem.Reset();
        economySystem.Reset();

        // Verify clean state
        Assert.AreEqual(0, session.CreepStore.ActiveCreeps.Count);
        Assert.AreEqual(0, session.TurretStore.ActiveTurrets.Count);
        Assert.AreEqual(0, session.ProjectileStore.ActiveProjectiles.Count);
        Assert.AreEqual(BASE_MAX_HEALTH, session.BaseStore.CurrentHealth);
        Assert.AreEqual(STARTING_COINS, session.EconomyStore.CurrentCoins);
        Assert.IsFalse(session.WaveStore.AllWavesCleared);
        Assert.AreEqual(-1, session.WaveStore.CurrentWaveIndex);
    }

    [Test]
    public void PostReset_SystemsTickCorrectly_FromFreshState()
    {
        var session = new GameSession(BASE_MAX_HEALTH, STARTING_COINS, TurretType.Regular);

        var waveSystem = new WaveSystem(session.WaveStore, session.CreepStore, WAVES);
        var spawnSystem = new SpawnSystem(session.CreepStore, session.WaveStore, SPAWN_POSITIONS, BASE_POSITION, CREEP_STATS);
        var placementInput = new PlacementInput();
        var placementSystem = new PlacementSystem(session.TurretStore, placementInput, session.EconomyStore, session.TurretSelectionStore, TURRET_STATS);
        var projectileSystem = new ProjectileSystem(session.TurretStore, session.CreepStore, session.ProjectileStore);
        var economySystem = new EconomySystem(session.EconomyStore, session.TurretStore, TURRET_STATS);

        // First session: tick to populate
        session.BeginFrame();
        waveSystem.Tick(DT);
        spawnSystem.Tick(DT);

        // Reset
        session.Reset();
        waveSystem.Reset();
        spawnSystem.Reset();
        placementSystem.Reset();
        projectileSystem.Reset();
        economySystem.Reset();

        // Second session: systems tick from wave 0, IDs from 0
        session.BeginFrame();
        waveSystem.Tick(DT);
        spawnSystem.Tick(DT);

        Assert.IsTrue(session.CreepStore.ActiveCreeps.Count > 0, "Creeps should spawn after reset");
        Assert.AreEqual(0, session.CreepStore.ActiveCreeps[0].Id, "Creep IDs should restart from 0");
        Assert.AreEqual(0, session.WaveStore.CurrentWaveIndex, "Wave index should restart from 0");
    }

    [Test]
    public void FullStateMachineRestartCycle_InitPlayingWinInitPlaying()
    {
        var homeBase = new GameObject("Base").AddComponent<HomeBaseComponent>();
        try
        {
            var stateMachine = new GameStateMachine();
            var session = new GameSession(BASE_MAX_HEALTH, STARTING_COINS, TurretType.Regular);

            var initState = new InitState(stateMachine.Fire, homeBase);
            var playingState = new PlayingState(stateMachine.Fire, session.BaseStore, session.WaveStore);
            var winState = new WinState(stateMachine.Fire);

            stateMachine.AddState(GameState.Init, initState);
            stateMachine.AddState(GameState.Playing, playingState);
            stateMachine.AddState(GameState.Win, winState);

            stateMachine.AddTransition(GameState.Init, GameTrigger.SceneValidated, GameState.Playing);
            stateMachine.AddTransition(GameState.Playing, GameTrigger.AllWavesCleared, GameState.Win);
            stateMachine.AddTransition(GameState.Win, GameTrigger.RestartRequested, GameState.Init);

            // Capture state changes for reset handler
            stateMachine.OnStateChanged += (from, to) =>
            {
                if (to == GameState.Init && (from == GameState.Win || from == GameState.Lose))
                {
                    session.Reset();
                }
            };

            // Init -> Playing
            stateMachine.Start(GameState.Init);
            stateMachine.Tick(DT);
            Assert.AreEqual(GameState.Playing, stateMachine.CurrentStateId);

            // Dirty state
            session.BaseStore.ApplyDamage(50);
            session.EconomyStore.AddCoins(200);

            // Playing -> Win
            session.WaveStore.MarkAllWavesCleared();
            stateMachine.Tick(DT); // PlayingState fires AllWavesCleared
            stateMachine.Tick(DT); // Resolves to Win
            Assert.AreEqual(GameState.Win, stateMachine.CurrentStateId);

            // Win -> Init (triggers reset via OnStateChanged handler)
            stateMachine.Fire(GameTrigger.RestartRequested);
            stateMachine.Tick(DT);
            Assert.AreEqual(GameState.Init, stateMachine.CurrentStateId);

            // Verify reset happened
            Assert.AreEqual(BASE_MAX_HEALTH, session.BaseStore.CurrentHealth);
            Assert.AreEqual(STARTING_COINS, session.EconomyStore.CurrentCoins);
            Assert.IsFalse(session.WaveStore.AllWavesCleared);

            // Init -> Playing (SceneValidated resolves next tick)
            stateMachine.Tick(DT);
            Assert.AreEqual(GameState.Playing, stateMachine.CurrentStateId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(homeBase.gameObject);
        }
    }
}
