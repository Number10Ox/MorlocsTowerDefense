using NUnit.Framework;
using UnityEngine;

public class TurretShootingIntegrationTests
{
    private const int BASE_MAX_HEALTH = 100;
    private const float TURRET_RANGE = 20f;
    private const float TURRET_FIRE_INTERVAL = 1f;
    private const int TURRET_DAMAGE = 1;
    private const float PROJECTILE_SPEED = 100f;
    private const int CREEP_MAX_HEALTH = 3;
    private const float CREEP_SPEED = 5f;
    private const int CREEP_DAMAGE_TO_BASE = 10;

    private CreepStore creepStore;
    private BaseStore baseStore;
    private TurretStore turretStore;
    private ProjectileStore projectileStore;
    private ProjectileSystem projectileSystem;
    private DamageSystem damageSystem;
    private MovementSystem movementSystem;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        baseStore = new BaseStore(BASE_MAX_HEALTH);
        turretStore = new TurretStore();
        projectileStore = new ProjectileStore();
        projectileSystem = new ProjectileSystem(turretStore, creepStore, projectileStore);
        damageSystem = new DamageSystem(creepStore, baseStore, projectileStore);
        movementSystem = new MovementSystem(creepStore);
    }

    private void BeginFrame()
    {
        creepStore.BeginFrame();
        baseStore.BeginFrame();
        turretStore.BeginFrame();
        projectileStore.BeginFrame();
    }

    private void TickCombat(float dt)
    {
        projectileSystem.Tick(dt);
        damageSystem.Tick(dt);
    }

    private CreepSimData AddCreep(int id, Vector3 position, int health = CREEP_MAX_HEALTH)
    {
        var creep = new CreepSimData(id)
        {
            Position = position,
            Target = Vector3.zero,
            Speed = CREEP_SPEED,
            DamageToBase = CREEP_DAMAGE_TO_BASE,
            Health = health,
            MaxHealth = health
        };
        creepStore.Add(creep);
        return creep;
    }

    private TurretSimData AddTurret(int id, Vector3 position)
    {
        var turret = new TurretSimData(id)
        {
            Position = position,
            Range = TURRET_RANGE,
            FireInterval = TURRET_FIRE_INTERVAL,
            Damage = TURRET_DAMAGE,
            ProjectileSpeed = PROJECTILE_SPEED,
            FireCooldown = 0f
        };
        turretStore.Add(turret);
        return turret;
    }

    // --- End-to-end: turret fires, projectile hits, creep takes damage ---

    [Test]
    public void TurretFiresProjectile_HitsCreep_ReducesHealth()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(0.3f, 0f, 0f));

        // Turret fires (cooldown starts at 0)
        TickCombat(0.016f);

        // Projectile spawned and hits within same tick (within hit threshold)
        Assert.AreEqual(1, projectileStore.HitsThisFrame.Count,
            "Projectile should hit immediately — creep within hit threshold");

        // Damage applied by DamageSystem in the same Tick
        Assert.IsTrue(creepStore.TryGetCreep(0, out CreepSimData creep));
        Assert.AreEqual(CREEP_MAX_HEALTH - TURRET_DAMAGE, creep.Health);
    }

    [Test]
    public void MultipleHits_KillCreep_CreepRemovedAfterBeginFrame()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(0.3f, 0f, 0f), health: 2);

        int killCount = 0;
        damageSystem.OnCreepKilled += (_, _) => killCount++;

        // First hit
        TickCombat(0.016f);
        Assert.IsTrue(creepStore.TryGetCreep(0, out CreepSimData creep));
        Assert.AreEqual(1, creep.Health);

        // Advance past fire interval so turret can fire again
        BeginFrame();
        TickCombat(TURRET_FIRE_INTERVAL + 0.001f);

        Assert.AreEqual(0, creep.Health);
        Assert.AreEqual(1, killCount);

        // Creep marked for removal, flushed on next BeginFrame
        BeginFrame();
        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(1, creepStore.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void FireIntervalRespected_NoFiringBeforeCooldownExpires()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(0.3f, 0f, 0f));

        // First tick fires and hits (creep within hit threshold)
        TickCombat(0.016f);
        Assert.AreEqual(1, projectileStore.HitsThisFrame.Count);

        // Clear frame, tick again before interval elapses
        BeginFrame();
        TickCombat(TURRET_FIRE_INTERVAL * 0.5f);
        Assert.AreEqual(0, projectileStore.HitsThisFrame.Count, "Should not fire before cooldown expires");

        // Complete the cooldown
        BeginFrame();
        TickCombat(TURRET_FIRE_INTERVAL * 0.5f + 0.001f);
        Assert.AreEqual(1, projectileStore.HitsThisFrame.Count, "Should fire after cooldown expires");
    }

    [Test]
    public void OutOfRangeCreep_NoProjectilesFired()
    {
        AddTurret(0, Vector3.zero);
        AddCreep(0, new Vector3(TURRET_RANGE + 10f, 0f, 0f));

        TickCombat(0.016f);

        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
        Assert.AreEqual(0, projectileStore.HitsThisFrame.Count);
    }

    [Test]
    public void CreepReachesBase_StillDealsDamage_WithoutTurrets()
    {
        var creep = AddCreep(0, Vector3.zero);
        creep.ReachedBase = true;

        damageSystem.Tick(0.016f);

        Assert.AreEqual(BASE_MAX_HEALTH - CREEP_DAMAGE_TO_BASE, baseStore.CurrentHealth);
    }

    [Test]
    public void DeadCreep_DoesNotDealBaseDamage()
    {
        var creep = AddCreep(0, Vector3.zero);
        creep.ReachedBase = true;
        creep.Health = 0;

        damageSystem.Tick(0.016f);

        Assert.AreEqual(BASE_MAX_HEALTH, baseStore.CurrentHealth);
        Assert.IsFalse(creep.HasDealtBaseDamage);
    }

    [Test]
    public void DeadCreep_DoesNotMove()
    {
        var creep = AddCreep(0, new Vector3(10f, 0f, 0f));
        creep.Health = 0;
        Vector3 startPos = creep.Position;

        movementSystem.Tick(1.0f);

        Assert.AreEqual(startPos, creep.Position);
        Assert.IsFalse(creep.ReachedBase);
    }

    [Test]
    public void FullPipeline_SpawnMoveShootKill()
    {
        // Simulate the full pipeline: spawn a creep near a turret, shoot it dead

        // Place turret at origin
        AddTurret(0, Vector3.zero);

        // Creep with 1 HP spawns near turret (far enough to survive movement, close enough for projectile overshoot)
        AddCreep(0, new Vector3(1f, 0f, 0f), health: 1);

        int killedId = -1;
        damageSystem.OnCreepKilled += (id, _) => killedId = id;

        // Phase 1: Movement (creep moves toward base at origin — already close)
        movementSystem.Tick(0.016f);

        // Phase 2: Combat
        TickCombat(0.016f);

        // Creep should be dead
        Assert.IsTrue(creepStore.TryGetCreep(0, out CreepSimData creep));
        Assert.AreEqual(0, creep.Health);
        Assert.AreEqual(0, killedId);

        // Next frame flushes removal
        BeginFrame();
        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
    }

    [Test]
    public void TwoTurrets_FireAtSameCreep_OnlyOneKillEvent()
    {
        // Two turrets both targeting the same 1-HP creep
        AddTurret(0, new Vector3(-1f, 0f, 0f));
        AddTurret(1, new Vector3(1f, 0f, 0f));
        AddCreep(0, new Vector3(0f, 0f, 0.3f), health: 1);

        int killCount = 0;
        damageSystem.OnCreepKilled += (_, _) => killCount++;

        TickCombat(0.016f);

        // Both turrets fire, but DamageSystem's guard prevents double-kill
        Assert.AreEqual(1, killCount);
    }

    [Test]
    public void ProjectileTargetDies_ProjectileRemoved()
    {
        // Slow projectile targeting a creep that dies before impact
        var turret = AddTurret(0, Vector3.zero);
        turret.ProjectileSpeed = 1f; // Very slow
        AddCreep(0, new Vector3(10f, 0f, 0f), health: 1);

        // Fire the projectile
        projectileSystem.Tick(0.016f);
        Assert.AreEqual(1, projectileStore.ActiveProjectiles.Count);

        // Kill the creep directly
        creepStore.TryGetCreep(0, out CreepSimData creep);
        creep.Health = 0;
        creepStore.MarkForRemoval(0);

        // Next frame: projectile should see dead target and self-remove
        BeginFrame();
        projectileSystem.Tick(0.016f);

        // Projectile marked for removal (will be flushed next BeginFrame)
        BeginFrame();
        Assert.AreEqual(0, projectileStore.ActiveProjectiles.Count);
    }

    [Test]
    public void BaseDamageRegression_StillWorksAlongsideCombat()
    {
        // Turret is present but creep reaches base — base damage path still works
        AddTurret(0, new Vector3(50f, 0f, 0f)); // Far from creep

        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = CREEP_SPEED,
            DamageToBase = CREEP_DAMAGE_TO_BASE,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        TickCombat(0.016f);

        Assert.AreEqual(BASE_MAX_HEALTH - CREEP_DAMAGE_TO_BASE, baseStore.CurrentHealth);
        Assert.IsTrue(creep.HasDealtBaseDamage);
    }
}
