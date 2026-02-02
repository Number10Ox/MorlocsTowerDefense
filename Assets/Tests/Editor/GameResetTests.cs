using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameResetTests
{
    private const int BASE_MAX_HEALTH = 100;
    private const int STARTING_COINS = 20;

    // --- GameSession.Reset ---

    [Test]
    public void GameSessionReset_RestoresAllStoresToInitialValues()
    {
        var session = new GameSession(BASE_MAX_HEALTH, STARTING_COINS, TurretType.Regular);

        // Dirty every store
        session.BaseStore.ApplyDamage(50);
        session.EconomyStore.AddCoins(100);
        session.CreepStore.Add(new CreepSimData(0) { Type = CreepType.Small, Position = Vector3.zero, Target = Vector3.one, Speed = 1f, Health = 10, MaxHealth = 10 });
        session.TurretStore.Add(new TurretSimData(0) { Type = TurretType.Regular, Position = Vector3.zero });
        session.ProjectileStore.Add(new ProjectileSimData(0) { Position = Vector3.zero, TargetCreepId = 0, Speed = 5f, Damage = 10 });
        session.WaveStore.StartWave(0);
        session.WaveStore.MarkAllWavesCleared();
        session.TurretSelectionStore.SelectType(TurretType.Freezing);

        session.Reset();

        Assert.AreEqual(BASE_MAX_HEALTH, session.BaseStore.CurrentHealth);
        Assert.IsFalse(session.BaseStore.IsDestroyed);
        Assert.AreEqual(STARTING_COINS, session.EconomyStore.CurrentCoins);
        Assert.AreEqual(0, session.CreepStore.ActiveCreeps.Count);
        Assert.AreEqual(0, session.TurretStore.ActiveTurrets.Count);
        Assert.AreEqual(0, session.ProjectileStore.ActiveProjectiles.Count);
        Assert.IsFalse(session.WaveStore.AllWavesCleared);
        Assert.AreEqual(-1, session.WaveStore.CurrentWaveIndex);
        Assert.AreEqual(TurretType.Regular, session.TurretSelectionStore.SelectedType);
    }

    // --- SpawnSystem.Reset ---

    [Test]
    public void SpawnSystemReset_RestartsCreepIdsFromZero()
    {
        var creepStore = new CreepStore();
        var waveStore = new WaveStore();
        var spawnPositions = new[] { Vector3.zero };
        var statsByType = new Dictionary<CreepType, CreepTypeStats>
        {
            { CreepType.Small, new CreepTypeStats(CreepType.Small, 2f, 1, 10, 5) }
        };

        var spawnSystem = new SpawnSystem(creepStore, waveStore, spawnPositions, Vector3.one, statsByType);

        // First session: spawn 3 creeps (IDs 0, 1, 2)
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Small);
        waveStore.EnqueueSpawn(CreepType.Small);
        spawnSystem.Tick(0.016f);
        Assert.AreEqual(3, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(2, creepStore.ActiveCreeps[2].Id);

        // Reset both store and system
        creepStore.Reset();
        spawnSystem.Reset();

        // Second session: IDs should start from 0 again
        waveStore.EnqueueSpawn(CreepType.Small);
        spawnSystem.Tick(0.016f);
        Assert.AreEqual(1, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(0, creepStore.ActiveCreeps[0].Id);
    }

    // --- EconomySystem.Reset ---

    [Test]
    public void EconomySystemReset_ClearsPendingCreditsBuffer()
    {
        var economyStore = new EconomyStore(STARTING_COINS);
        var turretStore = new TurretStore();
        var statsByType = new Dictionary<TurretType, TurretTypeStats>
        {
            { TurretType.Regular, new TurretTypeStats(TurretType.Regular, 5f, 1f, 10, 10f, 10) }
        };

        var economySystem = new EconomySystem(economyStore, turretStore, statsByType);

        // Buffer some kills
        economySystem.HandleCreepKilled(0, 5);
        economySystem.HandleCreepKilled(1, 10);

        // Reset before Tick — pending credits should be discarded
        economySystem.Reset();
        economyStore.Reset();
        turretStore.BeginFrame();

        economySystem.Tick(0.016f);

        Assert.AreEqual(STARTING_COINS, economyStore.CurrentCoins);
    }

    // --- Multiple sequential resets ---

    [Test]
    public void MultipleSequentialResets_ProduceConsistentCleanState()
    {
        var session = new GameSession(BASE_MAX_HEALTH, STARTING_COINS, TurretType.Regular);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            // Dirty stores each cycle with different amounts
            session.BaseStore.ApplyDamage(10 * (cycle + 1));
            session.EconomyStore.AddCoins(50 * (cycle + 1));
            session.CreepStore.Add(new CreepSimData(cycle) { Type = CreepType.Small, Position = Vector3.zero, Target = Vector3.one, Speed = 1f, Health = 10, MaxHealth = 10 });

            session.Reset();

            Assert.AreEqual(BASE_MAX_HEALTH, session.BaseStore.CurrentHealth, $"Cycle {cycle}: BaseStore health mismatch");
            Assert.AreEqual(STARTING_COINS, session.EconomyStore.CurrentCoins, $"Cycle {cycle}: EconomyStore coins mismatch");
            Assert.AreEqual(0, session.CreepStore.ActiveCreeps.Count, $"Cycle {cycle}: CreepStore not empty");
            Assert.AreEqual(0, session.TurretStore.ActiveTurrets.Count, $"Cycle {cycle}: TurretStore not empty");
            Assert.AreEqual(0, session.ProjectileStore.ActiveProjectiles.Count, $"Cycle {cycle}: ProjectileStore not empty");
            Assert.IsFalse(session.WaveStore.AllWavesCleared, $"Cycle {cycle}: WaveStore still marked cleared");
        }
    }
}
