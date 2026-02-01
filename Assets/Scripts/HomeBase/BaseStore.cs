using System;

public class BaseStore
{
    private readonly int maxHealth;
    private int currentHealth;
    private int damageTakenThisFrame;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDestroyed => currentHealth <= 0;
    public int DamageTakenThisFrame => damageTakenThisFrame;

    public event Action<int, int> OnBaseHealthChanged;

    public BaseStore(int maxHealth)
    {
        if (maxHealth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "Must be greater than zero.");
        }

        this.maxHealth = maxHealth;
        currentHealth = maxHealth;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0) return;
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        damageTakenThisFrame += amount;

        OnBaseHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void BeginFrame()
    {
        damageTakenThisFrame = 0;
    }

    public void Reset()
    {
        currentHealth = maxHealth;
        damageTakenThisFrame = 0;
        OnBaseHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
