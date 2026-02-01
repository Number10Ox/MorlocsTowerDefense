// Immutable value: target creep ID, damage, and slow effect from a projectile impact.
public readonly struct ProjectileHit
{
    public readonly int TargetCreepId;
    public readonly int Damage;
    public readonly float SlowDuration;
    public readonly float SlowMultiplier;

    public ProjectileHit(int targetCreepId, int damage, float slowDuration = 0f, float slowMultiplier = 1f)
    {
        TargetCreepId = targetCreepId;
        Damage = damage;
        SlowDuration = slowDuration;
        SlowMultiplier = slowMultiplier;
    }
}
