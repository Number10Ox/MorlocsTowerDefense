using UnityEngine;

// Per-projectile simulation state. Position updated by ProjectileSystem each tick.
public sealed class ProjectileSimData
{
    public int Id { get; }
    public Vector3 Position;
    public int TargetCreepId;
    public int Damage;
    public float Speed;

    public ProjectileSimData(int id)
    {
        Id = id;
    }
}
