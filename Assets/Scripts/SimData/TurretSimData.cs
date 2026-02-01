using UnityEngine;

// Per-turret simulation state. Combat stats set once at placement, FireCooldown updated by ProjectileSystem.
public sealed class TurretSimData
{
    public int Id { get; }
    public Vector3 Position;
    public float Range;
    public float FireInterval;
    public int Damage;
    public float ProjectileSpeed;
    public float FireCooldown;

    public TurretSimData(int id)
    {
        Id = id;
    }
}
