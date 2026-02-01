using UnityEngine;

// Per-projectile simulation state. Position updated by ProjectileSystem each tick.
// SlowDuration/SlowMultiplier propagated from turret at fire time for freeze projectiles.
public sealed class ProjectileSimData
{
    public int Id { get; }
    public Vector3 Position;
    public int TargetCreepId;
    public int Damage;
    public float Speed;
    public float SlowDuration;
    public float SlowMultiplier;

    public ProjectileSimData(int id)
    {
        Id = id;
    }
}
