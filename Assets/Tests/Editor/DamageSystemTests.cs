using System;
using NUnit.Framework;
using UnityEngine;

public class DamageSystemTests
{
    private const int BASE_MAX_HEALTH = 100;

    private CreepStore creepStore;
    private BaseStore baseStore;
    private ProjectileStore projectileStore;
    private DamageSystem damageSystem;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        baseStore = new BaseStore(BASE_MAX_HEALTH);
        projectileStore = new ProjectileStore();
        damageSystem = new DamageSystem(creepStore, baseStore, projectileStore);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullCreepStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DamageSystem(null, baseStore, projectileStore));
    }

    [Test]
    public void Constructor_NullBaseStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DamageSystem(creepStore, null, projectileStore));
    }

    [Test]
    public void Constructor_NullProjectileStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DamageSystem(creepStore, baseStore, null));
    }

    // --- Base Damage ---

    [Test]
    public void Tick_CreepReachedBase_DealsDamageToBase()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(90, baseStore.CurrentHealth);
    }

    [Test]
    public void Tick_CreepNotReachedBase_NoDamageToBase()
    {
        var creep = new CreepSimData(0)
        {
            Position = new Vector3(5f, 0f, 0f),
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = false,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(BASE_MAX_HEALTH, baseStore.CurrentHealth);
    }

    [Test]
    public void Tick_MultipleCreepsReachedBase_AllDealDamage()
    {
        for (int i = 0; i < 3; i++)
        {
            var creep = new CreepSimData(i)
            {
                Position = Vector3.zero,
                Target = Vector3.zero,
                Speed = 5f,
                DamageToBase = 10,
                ReachedBase = true,
                Health = 1,
                MaxHealth = 1
            };
            creepStore.Add(creep);
        }

        damageSystem.Tick(0.016f);

        Assert.AreEqual(70, baseStore.CurrentHealth);
    }

    [Test]
    public void Tick_MixedCreeps_OnlyReachedBaseCreepsDealDamage()
    {
        var arrived = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 25,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        var moving = new CreepSimData(1)
        {
            Position = new Vector3(5f, 0f, 0f),
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 25,
            ReachedBase = false,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(arrived);
        creepStore.Add(moving);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(75, baseStore.CurrentHealth);
    }

    [Test]
    public void Tick_NoCreeps_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => damageSystem.Tick(0.016f));
        Assert.AreEqual(BASE_MAX_HEALTH, baseStore.CurrentHealth);
    }

    [Test]
    public void Tick_CreepAlreadyDealtDamage_DoesNotDoubleDamage()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);
        Assert.AreEqual(90, baseStore.CurrentHealth);

        // Tick again — same creep still in ActiveCreeps (deferred removal)
        damageSystem.Tick(0.016f);
        Assert.AreEqual(90, baseStore.CurrentHealth);
    }

    [Test]
    public void Tick_CreepReachedBase_SetsHasDealtBaseDamage()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.IsTrue(creep.HasDealtBaseDamage);
    }

    [Test]
    public void Tick_BaseAlreadyDestroyed_StillIteratesButNoStateChange()
    {
        // Destroy base first
        baseStore.ApplyDamage(BASE_MAX_HEALTH);
        Assert.IsTrue(baseStore.IsDestroyed);

        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, baseStore.CurrentHealth);
        Assert.IsTrue(creep.HasDealtBaseDamage);
    }

    [Test]
    public void Tick_DeadCreepReachedBase_DoesNotDealBaseDamage()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = true,
            Health = 0,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(BASE_MAX_HEALTH, baseStore.CurrentHealth);
        Assert.IsFalse(creep.HasDealtBaseDamage);
    }

    // --- Projectile Hits ---

    [Test]
    public void Tick_ProjectileHit_ReducesCreepHealth()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Health = 5,
            MaxHealth = 5
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 2));

        damageSystem.Tick(0.016f);

        Assert.AreEqual(3, creep.Health);
    }

    [Test]
    public void Tick_ProjectileHit_CreepDies_MarkedForRemoval()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 1));

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, creep.Health);

        creepStore.BeginFrame();
        Assert.AreEqual(0, creepStore.ActiveCreeps.Count);
        Assert.AreEqual(1, creepStore.RemovedIdsThisFrame.Count);
    }

    [Test]
    public void Tick_ProjectileHit_CreepDies_FiresOnCreepKilled()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 1));

        int killedId = -1;
        damageSystem.OnCreepKilled += id => killedId = id;

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, killedId);
    }

    [Test]
    public void Tick_MultipleHitsSameCreep_OnlyFirstKills()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 1));
        projectileStore.RecordHit(new ProjectileHit(0, 1));

        int killCount = 0;
        damageSystem.OnCreepKilled += _ => killCount++;

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, creep.Health);
        Assert.AreEqual(1, killCount);
    }

    [Test]
    public void Tick_HitOnRemovedCreep_NoDamageApplied()
    {
        // Creep not in store
        projectileStore.RecordHit(new ProjectileHit(999, 5));

        Assert.DoesNotThrow(() => damageSystem.Tick(0.016f));
    }

    [Test]
    public void Tick_OverkillDamage_HealthClampsToZero()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Health = 2,
            MaxHealth = 2
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 100));

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, creep.Health);
    }

    [Test]
    public void Tick_DamageReducesHealthToExactlyZero_CreepDies()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Health = 3,
            MaxHealth = 3
        };
        creepStore.Add(creep);
        projectileStore.RecordHit(new ProjectileHit(0, 3));

        int killCount = 0;
        damageSystem.OnCreepKilled += _ => killCount++;

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, creep.Health);
        Assert.AreEqual(1, killCount);
    }

    [Test]
    public void Tick_BaseDamage_StillWorksWithProjectileStore()
    {
        // Ensure base damage path is unbroken by the projectile store addition
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 15,
            ReachedBase = true,
            Health = 1,
            MaxHealth = 1
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(85, baseStore.CurrentHealth);
    }
}
