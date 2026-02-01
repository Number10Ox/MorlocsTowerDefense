using System;
using NUnit.Framework;

public class PlayingStateTests
{
    private const int BASE_MAX_HEALTH = 100;

    private BaseStore baseStore;
    private GameTrigger? lastFiredTrigger;
    private int fireCount;
    private PlayingState state;

    [SetUp]
    public void SetUp()
    {
        baseStore = new BaseStore(BASE_MAX_HEALTH);
        lastFiredTrigger = null;
        fireCount = 0;

        state = new PlayingState(trigger =>
        {
            lastFiredTrigger = trigger;
            fireCount++;
        }, baseStore);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullFire_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayingState(null, baseStore));
    }

    [Test]
    public void Constructor_NullBaseStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayingState(trigger => { }, null));
    }

    // --- Tick end condition ---

    [Test]
    public void Tick_BaseDestroyed_FiresBaseDestroyedTrigger()
    {
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);

        state.Tick(0.016f);

        Assert.AreEqual(GameTrigger.BaseDestroyed, lastFiredTrigger);
    }

    [Test]
    public void Tick_BaseNotDestroyed_DoesNotFire()
    {
        state.Enter();
        baseStore.ApplyDamage(10);

        state.Tick(0.016f);

        Assert.IsNull(lastFiredTrigger);
        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void Tick_BaseDestroyed_FiresOnlyOnce()
    {
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);

        state.Tick(0.016f);
        state.Tick(0.016f);
        state.Tick(0.016f);

        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void Enter_ResetsGuard_AllowsRefireAfterReenter()
    {
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);

        state.Tick(0.016f);
        Assert.AreEqual(1, fireCount);

        // Simulate re-entering after reset
        baseStore.Reset();
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);

        state.Tick(0.016f);
        Assert.AreEqual(2, fireCount);
    }

    [Test]
    public void Tick_BaseHealthy_NoTriggerFired()
    {
        state.Enter();

        state.Tick(0.016f);

        Assert.IsNull(lastFiredTrigger);
    }
}
