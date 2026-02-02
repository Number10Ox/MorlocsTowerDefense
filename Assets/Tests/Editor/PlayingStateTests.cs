using System;
using NUnit.Framework;

public class PlayingStateTests
{
    private const int BASE_MAX_HEALTH = 100;

    private BaseStore baseStore;
    private WaveStore waveStore;
    private GameTrigger? lastFiredTrigger;
    private int fireCount;
    private PlayingState state;

    [SetUp]
    public void SetUp()
    {
        baseStore = new BaseStore(BASE_MAX_HEALTH);
        waveStore = new WaveStore();
        lastFiredTrigger = null;
        fireCount = 0;

        state = new PlayingState(trigger =>
        {
            lastFiredTrigger = trigger;
            fireCount++;
        }, baseStore, waveStore);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_NullFire_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayingState(null, baseStore, waveStore));
    }

    [Test]
    public void Constructor_NullBaseStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayingState(trigger => { }, null, waveStore));
    }

    [Test]
    public void Constructor_NullWaveStore_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlayingState(trigger => { }, baseStore, null));
    }

    // --- Base destroyed ---

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
    public void Enter_ResetsBaseDestroyedGuard_AllowsRefireAfterReenter()
    {
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);

        state.Tick(0.016f);
        Assert.AreEqual(1, fireCount);

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

    // --- All waves cleared ---

    [Test]
    public void Tick_AllWavesCleared_FiresAllWavesClearedTrigger()
    {
        state.Enter();
        waveStore.MarkAllWavesCleared();

        state.Tick(0.016f);

        Assert.AreEqual(GameTrigger.AllWavesCleared, lastFiredTrigger);
    }

    [Test]
    public void Tick_WavesNotCleared_DoesNotFire()
    {
        state.Enter();

        state.Tick(0.016f);

        Assert.IsNull(lastFiredTrigger);
    }

    [Test]
    public void Tick_AllWavesCleared_FiresOnlyOnce()
    {
        state.Enter();
        waveStore.MarkAllWavesCleared();

        state.Tick(0.016f);
        state.Tick(0.016f);
        state.Tick(0.016f);

        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void Enter_ResetsWavesClearedGuard_AllowsRefireAfterReenter()
    {
        state.Enter();
        waveStore.MarkAllWavesCleared();

        state.Tick(0.016f);
        Assert.AreEqual(1, fireCount);

        waveStore.Reset();
        state.Enter();
        waveStore.MarkAllWavesCleared();

        state.Tick(0.016f);
        Assert.AreEqual(2, fireCount);
    }

    // --- Priority: lose beats win ---

    [Test]
    public void Tick_BothConditions_BaseDestroyedTakesPriority()
    {
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);
        waveStore.MarkAllWavesCleared();

        state.Tick(0.016f);

        Assert.AreEqual(GameTrigger.BaseDestroyed, lastFiredTrigger);
        Assert.AreEqual(1, fireCount);
    }

    [Test]
    public void Tick_BothConditions_WavesClearedNotFiredIfBaseDestroyed()
    {
        state.Enter();
        baseStore.ApplyDamage(BASE_MAX_HEALTH);
        waveStore.MarkAllWavesCleared();

        state.Tick(0.016f);
        state.Tick(0.016f);

        // BaseDestroyed fires once, AllWavesCleared never fires
        Assert.AreEqual(1, fireCount);
        Assert.AreEqual(GameTrigger.BaseDestroyed, lastFiredTrigger);
    }
}
