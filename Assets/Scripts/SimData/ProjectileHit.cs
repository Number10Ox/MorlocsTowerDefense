// Immutable value: target creep ID and damage from a projectile impact.
public readonly struct ProjectileHit
{
    public readonly int TargetCreepId;
    public readonly int Damage;

    public ProjectileHit(int targetCreepId, int damage)
    {
        TargetCreepId = targetCreepId;
        Damage = damage;
    }
}
