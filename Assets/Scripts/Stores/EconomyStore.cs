using System;

// Authoritative coin balance state. Single writer: EconomySystem.
// Fires OnCoinsChanged for presentation. Allows 0 starting coins.
public class EconomyStore
{
    private readonly int startingCoins;
    private int currentCoins;
    private int coinsEarnedThisFrame;
    private int coinsSpentThisFrame;

    public int CurrentCoins => currentCoins;
    public int CoinsEarnedThisFrame => coinsEarnedThisFrame;
    public int CoinsSpentThisFrame => coinsSpentThisFrame;

    public event Action<int> OnCoinsChanged;

    public EconomyStore(int startingCoins)
    {
        if (startingCoins < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startingCoins), "Must be zero or greater.");
        }

        this.startingCoins = startingCoins;
        currentCoins = startingCoins;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        currentCoins += amount;
        coinsEarnedThisFrame += amount;
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (currentCoins < amount) return false;

        currentCoins -= amount;
        coinsSpentThisFrame += amount;
        OnCoinsChanged?.Invoke(currentCoins);
        return true;
    }

    public bool CanAfford(int cost)
    {
        return currentCoins >= cost;
    }

    public void BeginFrame()
    {
        coinsEarnedThisFrame = 0;
        coinsSpentThisFrame = 0;
    }

    public void Reset()
    {
        currentCoins = startingCoins;
        coinsEarnedThisFrame = 0;
        coinsSpentThisFrame = 0;
        OnCoinsChanged?.Invoke(currentCoins);
    }
}
