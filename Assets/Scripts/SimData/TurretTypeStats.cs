// Per-turret-type combat and economy stats. Built once at bootstrap from TurretDefinitions entries.
// Single source of truth: passed via IReadOnlyDictionary to PlacementSystem, EconomySystem, and TurretSelectionHud.
public readonly struct TurretTypeStats
{
    public readonly TurretType Type;
    public readonly float Range;
    public readonly float FireInterval;
    public readonly int Damage;
    public readonly float ProjectileSpeed;
    public readonly int Cost;
    public readonly float SlowDuration;
    public readonly float SlowMultiplier;

    public TurretTypeStats(
        TurretType type,
        float range,
        float fireInterval,
        int damage,
        float projectileSpeed,
        int cost,
        float slowDuration = 0f,
        float slowMultiplier = 1f)
    {
        Type = type;
        Range = range;
        FireInterval = fireInterval;
        Damage = damage;
        ProjectileSpeed = projectileSpeed;
        Cost = cost;
        SlowDuration = slowDuration;
        SlowMultiplier = slowMultiplier;
    }
}
