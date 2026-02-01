using System;
using NUnit.Framework;
using UnityEngine;

public class DamageSystemTests
{
    private const int BASE_MAX_HEALTH = 100;

    private CreepStore creepStore;
    private BaseStore baseStore;
    private DamageSystem damageSystem;

    [SetUp]
    public void SetUp()
    {
        creepStore = new CreepStore();
        baseStore = new BaseStore(BASE_MAX_HEALTH);
        damageSystem = new DamageSystem(creepStore, baseStore);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullCreepStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DamageSystem(null, baseStore));
    }

    [Test]
    public void Constructor_NullBaseStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DamageSystem(creepStore, null));
    }

    // --- Tick ---

    [Test]
    public void Tick_CreepReachedBase_DealsDamageToBase()
    {
        var creep = new CreepSimData(0)
        {
            Position = Vector3.zero,
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 10,
            ReachedBase = true
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
            ReachedBase = false
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
                ReachedBase = true
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
            ReachedBase = true
        };
        var moving = new CreepSimData(1)
        {
            Position = new Vector3(5f, 0f, 0f),
            Target = Vector3.zero,
            Speed = 5f,
            DamageToBase = 25,
            ReachedBase = false
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
            ReachedBase = true
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
            ReachedBase = true
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
            ReachedBase = true
        };
        creepStore.Add(creep);

        damageSystem.Tick(0.016f);

        Assert.AreEqual(0, baseStore.CurrentHealth);
        Assert.IsTrue(creep.HasDealtBaseDamage);
    }
}
