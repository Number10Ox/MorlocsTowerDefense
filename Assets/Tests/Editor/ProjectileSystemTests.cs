using System;
using NUnit.Framework;
using UnityEngine;

public class ProjectileSystemTests
{
    private const float DEFAULT_RANGE = 10f;
    private const float DEFAULT_FIRE_INTERVAL = 1f;
    private const int DEFAULT_DAMAGE = 1;
    private const float DEFAULT_PROJECTILE_SPEED = 20f;
    private const int DEFAULT_CREEP_HEALTH = 3;

    private TurretStore turretStore;
    private CreepStore creepStore;
    private ProjectileStore projectileStore;
    private ProjectileSystem system;

    [SetUp]
    public void SetUp()
    {
        turretStore = new TurretStore();
        creepStore = new CreepStore();
        projectileStore = new ProjectileStore();
        system = new ProjectileSystem(turretStore, creepStore, projectileStore);
    }

    private TurretSimData AddTurret(int id, Vector3 position,
        float range = DEFAULT_RANGE,
        float fireInterval = DEFAULT_FIRE_INTERVAL,
        int damage = DEFAULT_DAMAGE,
        float projectileSpeed = DEFAULT_PROJECTILE_SPEED)
    {
        var turret = new TurretSimData(id)
        {
            Position = position,
            Range = range,
            FireInterval = fireInterval,
            Damage = damage,
            ProjectileSpeed = projectileSpeed
        };
        turretStore.Add(turret);
        return turret;
    }

    private CreepSimData AddCreep(int id, Vector3 position, int health = DEFAULT_CREEP_HEALTH)
    {
        var creep = new CreepSimData(id)
        {
            Position = position,
            Target = Vector3.zero,
            Speed = 5f,
            Health = health,
            MaxHealth = health
        };
        creepStore.Add(creep);
        return creep;
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullTurretStore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectileSystem(null, creepStore, projectileStore));
    }

    [Test]
    public void Constructor_NullCreepStore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectileSystem(turretStore, null, projectileStore));
    }

    [Test]
    public void Constructor_NullProjectileStore_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProjectileSystem(turretStore, creepStore, null));
    }

    // --- Firing ---

    [Test]
    public void Tick_TurretWithCreepInRange_FiresProjectile()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0.016f);

        Assert.AreEqual(1, projectileStore.ActiveProjectiles.Count);
        Assert.AreEqual(1, projectileStore.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_TurretNoCreepInRange_DoesNotFire()
    {
        AddTurret(0, Vector3.zero, range: 5f);
        AddCreep(0, new Vector3(20f, 0f, 0f));

        system.Tick(0.016f);

        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_TurretNoCreeps_DoesNotFire()
    {
        AddTurret(0, Vector3.zero);

        system.Tick(0.016f);

        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_FireCooldownNotReady_DoesNotFire()
    {
        var turret = AddTurret(0, Vector3.zero, fireInterval: 1f);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        // First tick fires (cooldown starts at 0)
        system.Tick(0.016f);
        Assert.AreEqual(1, projectileStore.ActiveProjectiles.Count);

        // Remove the projectile so we can count cleanly
        projectileStore.BeginFrame();

        // Second tick — cooldown not expired yet
        system.Tick(0.016f);
        Assert.AreEqual(0, projectileStore.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_AfterFiring_CooldownResetsToFireInterval()
    {
        var turret = AddTurret(0, Vector3.zero, fireInterval: 2f);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0.016f);

        // Cooldown should be approximately fireInterval (2.0) minus the small dt that was subtracted
        Assert.Greater(turret.FireCooldown, 1.9f);
    }

    [Test]
    public void Tick_CooldownExpires_FiresAgain()
    {
        AddTurret(0, Vector3.zero, fireInterval: 0.5f);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        // First shot
        system.Tick(0.016f);
        Assert.AreEqual(1, projectileStore.SpawnedThisFrame.Count);

        projectileStore.BeginFrame();

        // Wait for cooldown
        system.Tick(0.5f);
        Assert.AreEqual(1, projectileStore.SpawnedThisFrame.Count);
    }

    [Test]
    public void Tick_MultipleCreeps_TargetsNearest()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(10, new Vector3(8f, 0f, 0f));
        AddCreep(11, new Vector3(3f, 0f, 0f));

        system.Tick(0.016f);

        var proj = projectileStore.ActiveProjectiles[0];
        Assert.AreEqual(11, proj.TargetCreepId);
    }

    [Test]
    public void Tick_CreepExactlyAtRangeEdge_CanBeTargeted()
    {
        AddTurret(0, Vector3.zero, range: 5f);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0.016f);

        Assert.AreEqual(1, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_DeadCreepInRange_NotTargeted()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(3f, 0f, 0f), health: 0);

        system.Tick(0.016f);

        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_CreepReachedBase_NotTargeted()
    {
        AddTurret(0, Vector3.zero);
        var creep = AddCreep(0, new Vector3(3f, 0f, 0f));
        creep.ReachedBase = true;

        system.Tick(0.016f);

        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_ProjectileSpawnsAtTurretPosition()
    {
        var turretPos = new Vector3(2f, 1f, 3f);
        AddTurret(0, turretPos);
        AddCreep(0, new Vector3(2.3f, 1f, 3f));

        system.Tick(0.016f);

        Assert.AreEqual(turretPos, projectileStore.ActiveProjectiles[0].Position);
    }

    [Test]
    public void Tick_ProjectileHasCorrectDamageAndSpeed()
    {
        AddTurret(0, Vector3.zero, damage: 5, projectileSpeed: 25f);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0.016f);

        var proj = projectileStore.ActiveProjectiles[0];
        Assert.AreEqual(5, proj.Damage);
        Assert.AreEqual(25f, proj.Speed, 0.001f);
    }

    // --- Movement ---

    [Test]
    public void Tick_ProjectileMovesTowardTarget()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(10f, 0f, 0f));

        // Fire
        system.Tick(0.016f);
        projectileStore.BeginFrame();

        // Move
        system.Tick(0.1f);

        var proj = projectileStore.ActiveProjectiles[0];
        Assert.Greater(proj.Position.x, 0f);
        Assert.Less(proj.Position.x, 10f);
    }

    [Test]
    public void Tick_ProjectileReachesTarget_RecordsHit()
    {
        AddTurret(0, Vector3.zero, projectileSpeed: 100f, fireInterval: 10f);
        AddCreep(0, new Vector3(2f, 0f, 0f));

        // Fire
        system.Tick(0.016f);
        projectileStore.BeginFrame();

        // Move with large speed — should hit
        system.Tick(1f);

        Assert.AreEqual(1, projectileStore.HitsThisFrame.Count);
        Assert.AreEqual(0, projectileStore.HitsThisFrame[0].TargetCreepId);
    }

    [Test]
    public void Tick_ProjectileHit_MarkedForRemoval()
    {
        AddTurret(0, Vector3.zero, projectileSpeed: 100f);
        AddCreep(0, new Vector3(2f, 0f, 0f));

        // Fire
        system.Tick(0.016f);
        projectileStore.BeginFrame();

        // Hit
        system.Tick(1f);

        // Projectile marked for removal — still in active until flush
        projectileStore.BeginFrame();
        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_TargetRemoved_ProjectileRemoved()
    {
        AddTurret(0, Vector3.zero, projectileSpeed: 1f, range: 200f);
        AddCreep(0, new Vector3(100f, 0f, 0f));

        // Fire
        system.Tick(0.016f);
        Assert.AreEqual(1, projectileStore.ActiveProjectiles.Count);

        // Remove creep
        creepStore.MarkForRemoval(0);
        creepStore.BeginFrame();
        projectileStore.BeginFrame();

        // Projectile should detect target is gone
        system.Tick(0.016f);

        projectileStore.BeginFrame();
        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_TargetDead_ProjectileRemoved()
    {
        AddTurret(0, Vector3.zero, projectileSpeed: 1f, range: 200f);
        var creep = AddCreep(0, new Vector3(100f, 0f, 0f));

        // Fire
        system.Tick(0.016f);
        Assert.AreEqual(1, projectileStore.ActiveProjectiles.Count);

        // Kill creep (simulate damage from another system)
        creep.Health = 0;
        projectileStore.BeginFrame();

        // Projectile should detect target is dead
        system.Tick(0.016f);

        projectileStore.BeginFrame();
        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_ZeroDeltaTime_DoesNothing()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0f);

        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void Tick_ProjectileOvershootsTarget_StillHits()
    {
        AddTurret(0, Vector3.zero, projectileSpeed: 1000f);
        AddCreep(0, new Vector3(2f, 0f, 0f));

        // Fire
        system.Tick(0.016f);
        projectileStore.BeginFrame();

        // Large step — projectile would overshoot
        system.Tick(10f);

        Assert.AreEqual(1, projectileStore.HitsThisFrame.Count);
    }

    [Test]
    public void Tick_TurretVeryCloseToCreep_HitsSameFrame()
    {
        // Turret and creep within hit threshold
        AddTurret(0, Vector3.zero, projectileSpeed: 100f);
        AddCreep(0, new Vector3(0.1f, 0f, 0f));

        // Fire and move in same tick — should hit immediately
        system.Tick(0.016f);

        Assert.AreEqual(1, projectileStore.HitsThisFrame.Count);
    }

    [Test]
    public void Tick_MultipleProjectiles_AllAdvance()
    {
        AddTurret(0, Vector3.zero, fireInterval: 0.01f, range: 200f);
        AddCreep(0, new Vector3(50f, 0f, 0f));

        // Fire two projectiles
        system.Tick(0.016f);
        projectileStore.BeginFrame();
        system.Tick(0.016f);
        projectileStore.BeginFrame();

        Assert.AreEqual(2, projectileStore.ActiveProjectiles.Count);

        // Both should move
        system.Tick(0.1f);

        for (int i = 0; i < projectileStore.ActiveProjectiles.Count; i++)
        {
            Assert.Greater(projectileStore.ActiveProjectiles[i].Position.x, 0f);
        }
    }

    [Test]
    public void Reset_ClearsProjectileIdCounter()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0.016f);
        int firstId = projectileStore.ActiveProjectiles[0].Id;

        projectileStore.Reset();
        turretStore.Reset();
        creepStore.Reset();
        system.Reset();

        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(5f, 0f, 0f));

        system.Tick(0.016f);
        int secondId = projectileStore.ActiveProjectiles[0].Id;

        Assert.AreEqual(firstId, secondId);
    }

    [Test]
    public void Tick_MultipleTurrets_EachFiresIndependently()
    {
        AddTurret(0, Vector3.zero);
        AddTurret(1, new Vector3(0f, 0f, 10f));
        AddCreep(0, new Vector3(5f, 0f, 0f));
        AddCreep(1, new Vector3(0f, 0f, 15f));

        system.Tick(0.016f);

        Assert.AreEqual(2, projectileStore.ActiveProjectiles.Count);
    }
}
