using System;
using NUnit.Framework;

public class EconomyStoreTests
{
    private const int DEFAULT_STARTING_COINS = 20;

    private EconomyStore store;

    [SetUp]
    public void SetUp()
    {
        store = new EconomyStore(DEFAULT_STARTING_COINS);
    }

    // --- Constructor ---

    [Test]
    public void Constructor_SetsCurrentCoins()
    {
        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void Constructor_ZeroStartingCoins_Allowed()
    {
        var zeroStore = new EconomyStore(0);

        Assert.AreEqual(0, zeroStore.CurrentCoins);
    }

    [Test]
    public void Constructor_NegativeStartingCoins_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EconomyStore(-1));
    }

    // --- AddCoins ---

    [Test]
    public void AddCoins_IncreasesBalance()
    {
        store.AddCoins(5);

        Assert.AreEqual(25, store.CurrentCoins);
    }

    [Test]
    public void AddCoins_MultipleCalls_Accumulates()
    {
        store.AddCoins(3);
        store.AddCoins(7);

        Assert.AreEqual(30, store.CurrentCoins);
    }

    [Test]
    public void AddCoins_ZeroAmount_NoChange()
    {
        store.AddCoins(0);

        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void AddCoins_NegativeAmount_NoChange()
    {
        store.AddCoins(-5);

        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void AddCoins_FiresOnCoinsChanged()
    {
        int capturedBalance = -1;
        store.OnCoinsChanged += balance => capturedBalance = balance;

        store.AddCoins(10);

        Assert.AreEqual(30, capturedBalance);
    }

    [Test]
    public void AddCoins_ZeroAmount_DoesNotFireEvent()
    {
        int fireCount = 0;
        store.OnCoinsChanged += _ => fireCount++;

        store.AddCoins(0);

        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void AddCoins_TracksCoinsEarnedThisFrame()
    {
        store.AddCoins(3);
        store.AddCoins(7);

        Assert.AreEqual(10, store.CoinsEarnedThisFrame);
    }

    // --- TrySpendCoins ---

    [Test]
    public void TrySpendCoins_SufficientFunds_ReturnsTrue()
    {
        bool result = store.TrySpendCoins(5);

        Assert.IsTrue(result);
    }

    [Test]
    public void TrySpendCoins_SufficientFunds_DeductsBalance()
    {
        store.TrySpendCoins(5);

        Assert.AreEqual(15, store.CurrentCoins);
    }

    [Test]
    public void TrySpendCoins_ExactBalance_ReturnsTrue()
    {
        bool result = store.TrySpendCoins(DEFAULT_STARTING_COINS);

        Assert.IsTrue(result);
        Assert.AreEqual(0, store.CurrentCoins);
    }

    [Test]
    public void TrySpendCoins_InsufficientFunds_ReturnsFalse()
    {
        bool result = store.TrySpendCoins(DEFAULT_STARTING_COINS + 1);

        Assert.IsFalse(result);
    }

    [Test]
    public void TrySpendCoins_InsufficientFunds_NoMutation()
    {
        store.TrySpendCoins(DEFAULT_STARTING_COINS + 1);

        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void TrySpendCoins_InsufficientFunds_DoesNotFireEvent()
    {
        int fireCount = 0;
        store.OnCoinsChanged += _ => fireCount++;

        store.TrySpendCoins(DEFAULT_STARTING_COINS + 1);

        Assert.AreEqual(0, fireCount);
    }

    [Test]
    public void TrySpendCoins_ZeroAmount_ReturnsFalse()
    {
        bool result = store.TrySpendCoins(0);

        Assert.IsFalse(result);
        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void TrySpendCoins_NegativeAmount_ReturnsFalse()
    {
        bool result = store.TrySpendCoins(-5);

        Assert.IsFalse(result);
        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void TrySpendCoins_FiresOnCoinsChanged()
    {
        int capturedBalance = -1;
        store.OnCoinsChanged += balance => capturedBalance = balance;

        store.TrySpendCoins(5);

        Assert.AreEqual(15, capturedBalance);
    }

    [Test]
    public void TrySpendCoins_TracksCoinsSpentThisFrame()
    {
        store.TrySpendCoins(5);
        store.TrySpendCoins(3);

        Assert.AreEqual(8, store.CoinsSpentThisFrame);
    }

    // --- CanAfford ---

    [Test]
    public void CanAfford_SufficientFunds_ReturnsTrue()
    {
        Assert.IsTrue(store.CanAfford(DEFAULT_STARTING_COINS));
    }

    [Test]
    public void CanAfford_InsufficientFunds_ReturnsFalse()
    {
        Assert.IsFalse(store.CanAfford(DEFAULT_STARTING_COINS + 1));
    }

    [Test]
    public void CanAfford_ZeroCost_ReturnsTrue()
    {
        Assert.IsTrue(store.CanAfford(0));
    }

    // --- BeginFrame ---

    [Test]
    public void BeginFrame_ClearsCoinsEarnedThisFrame()
    {
        store.AddCoins(10);
        Assert.AreEqual(10, store.CoinsEarnedThisFrame);

        store.BeginFrame();

        Assert.AreEqual(0, store.CoinsEarnedThisFrame);
    }

    [Test]
    public void BeginFrame_ClearsCoinsSpentThisFrame()
    {
        store.TrySpendCoins(5);
        Assert.AreEqual(5, store.CoinsSpentThisFrame);

        store.BeginFrame();

        Assert.AreEqual(0, store.CoinsSpentThisFrame);
    }

    [Test]
    public void BeginFrame_DoesNotAffectBalance()
    {
        store.AddCoins(10);

        store.BeginFrame();

        Assert.AreEqual(30, store.CurrentCoins);
    }

    // --- Reset ---

    [Test]
    public void Reset_RestoresStartingCoins()
    {
        store.TrySpendCoins(15);
        store.AddCoins(3);

        store.Reset();

        Assert.AreEqual(DEFAULT_STARTING_COINS, store.CurrentCoins);
    }

    [Test]
    public void Reset_ClearsFrameCounters()
    {
        store.AddCoins(5);
        store.TrySpendCoins(3);

        store.Reset();

        Assert.AreEqual(0, store.CoinsEarnedThisFrame);
        Assert.AreEqual(0, store.CoinsSpentThisFrame);
    }

    [Test]
    public void Reset_FiresOnCoinsChanged()
    {
        store.TrySpendCoins(10);

        int capturedBalance = -1;
        store.OnCoinsChanged += balance => capturedBalance = balance;

        store.Reset();

        Assert.AreEqual(DEFAULT_STARTING_COINS, capturedBalance);
    }
}
