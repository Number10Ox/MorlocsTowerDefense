using System;
using NUnit.Framework;

public class BaseStoreTests
{
    private const int DEFAULT_MAX_HEALTH = 100;

    private BaseStore store;

    [SetUp]
    public void SetUp()
    {
        store = new BaseStore(DEFAULT_MAX_HEALTH);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_SetsMaxHealthAndCurrentHealth()
    {
        Assert.AreEqual(DEFAULT_MAX_HEALTH, store.MaxHealth);
        Assert.AreEqual(DEFAULT_MAX_HEALTH, store.CurrentHealth);
    }

    [Test]
    public void Constructor_ZeroMaxHealth_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BaseStore(0));
    }

    [Test]
    public void Constructor_NegativeMaxHealth_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BaseStore(-5));
    }

    // --- ApplyDamage ---

    [Test]
    public void ApplyDamage_ReducesCurrentHealth()
    {
        store.ApplyDamage(10);

        Assert.AreEqual(90, store.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_MultipleCalls_AccumulatesDamage()
    {
        store.ApplyDamage(20);
        store.ApplyDamage(30);

        Assert.AreEqual(50, store.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_ExceedsHealth_ClampsToZero()
    {
        store.ApplyDamage(150);

        Assert.AreEqual(0, store.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_ZeroAmount_NoChange()
    {
        store.ApplyDamage(0);

        Assert.AreEqual(DEFAULT_MAX_HEALTH, store.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_NegativeAmount_NoChange()
    {
        store.ApplyDamage(-5);

        Assert.AreEqual(DEFAULT_MAX_HEALTH, store.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_AlreadyDestroyed_NoFurtherChange()
    {
        store.ApplyDamage(DEFAULT_MAX_HEALTH);
        Assert.AreEqual(0, store.CurrentHealth);

        store.ApplyDamage(10);

        Assert.AreEqual(0, store.CurrentHealth);
    }

    [Test]
    public void ApplyDamage_FiresOnBaseHealthChanged()
    {
        int capturedCurrent = -1;
        int capturedMax = -1;
        store.OnBaseHealthChanged += (current, max) =>
        {
            capturedCurrent = current;
            capturedMax = max;
        };

        store.ApplyDamage(25);

        Assert.AreEqual(75, capturedCurrent);
        Assert.AreEqual(DEFAULT_MAX_HEALTH, capturedMax);
    }

    [Test]
    public void ApplyDamage_AlreadyDestroyed_DoesNotFireEvent()
    {
        store.ApplyDamage(DEFAULT_MAX_HEALTH);

        int fireCount = 0;
        store.OnBaseHealthChanged += (current, max) => fireCount++;

        store.ApplyDamage(10);

        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void ApplyDamage_ZeroAmount_DoesNotFireEvent()
    {
        int fireCount = 0;
        store.OnBaseHealthChanged += (current, max) => fireCount++;

        store.ApplyDamage(0);

        Assert.AreEqual(0, fireCount);
    }

    // --- IsDestroyed ---

    [Test]
    public void IsDestroyed_HealthAboveZero_ReturnsFalse()
    {
        Assert.IsFalse(store.IsDestroyed);
    }

    [Test]
    public void IsDestroyed_HealthEqualsZero_ReturnsTrue()
    {
        store.ApplyDamage(DEFAULT_MAX_HEALTH);

        Assert.IsTrue(store.IsDestroyed);
    }

    // --- DamageTakenThisFrame ---

    [Test]
    public void DamageTakenThisFrame_AccumulatesWithinFrame()
    {
        store.ApplyDamage(10);
        store.ApplyDamage(20);

        Assert.AreEqual(30, store.DamageTakenThisFrame);
    }

    [Test]
    public void DamageTakenThisFrame_ZeroByDefault()
    {
        Assert.AreEqual(0, store.DamageTakenThisFrame);
    }

    [Test]
    public void DamageTakenThisFrame_OverkillDamage_TracksEffectiveDamageOnly()
    {
        store.ApplyDamage(70);
        store.BeginFrame();

        // 30 HP remaining, deal 50 damage — only 30 is effective
        store.ApplyDamage(50);

        Assert.AreEqual(30, store.DamageTakenThisFrame);
        Assert.AreEqual(0, store.CurrentHealth);
    }

    [Test]
    public void DamageTakenThisFrame_AlreadyDestroyed_DoesNotAccumulate()
    {
        store.ApplyDamage(DEFAULT_MAX_HEALTH);
        store.BeginFrame();

        store.ApplyDamage(10);

        Assert.AreEqual(0, store.DamageTakenThisFrame);
    }

    // --- BeginFrame ---

    [Test]
    public void BeginFrame_ClearsDamageTakenThisFrame()
    {
        store.ApplyDamage(15);
        Assert.AreEqual(15, store.DamageTakenThisFrame);

        store.BeginFrame();

        Assert.AreEqual(0, store.DamageTakenThisFrame);
    }

    [Test]
    public void BeginFrame_DoesNotAffectHealth()
    {
        store.ApplyDamage(30);

        store.BeginFrame();

        Assert.AreEqual(70, store.CurrentHealth);
    }

    // --- Reset ---

    [Test]
    public void Reset_RestoresHealthToMax()
    {
        store.ApplyDamage(60);

        store.Reset();

        Assert.AreEqual(DEFAULT_MAX_HEALTH, store.CurrentHealth);
        Assert.IsFalse(store.IsDestroyed);
    }

    [Test]
    public void Reset_ClearsDamageTakenThisFrame()
    {
        store.ApplyDamage(10);

        store.Reset();

        Assert.AreEqual(0, store.DamageTakenThisFrame);
    }

    [Test]
    public void Reset_FiresOnBaseHealthChanged()
    {
        store.ApplyDamage(50);

        int capturedCurrent = -1;
        int capturedMax = -1;
        store.OnBaseHealthChanged += (current, max) =>
        {
            capturedCurrent = current;
            capturedMax = max;
        };

        store.Reset();

        Assert.AreEqual(DEFAULT_MAX_HEALTH, capturedCurrent);
        Assert.AreEqual(DEFAULT_MAX_HEALTH, capturedMax);
    }
}
