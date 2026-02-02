// Per-creep-type stats. Built once at bootstrap from CreepDefinitions entries.
// Passed as CreepTypeStats[] to SpawnSystem. Immutable after creation.
public readonly struct CreepTypeStats
{
    public readonly CreepType Type;
    public readonly float Speed;
    public readonly int DamageToBase;
    public readonly int MaxHealth;
    public readonly int CoinReward;

    public CreepTypeStats(
        CreepType type,
        float speed,
        int damageToBase,
        int maxHealth,
        int coinReward)
    {
        Type = type;
        Speed = speed;
        DamageToBase = damageToBase;
        MaxHealth = maxHealth;
        CoinReward = coinReward;
    }
}
