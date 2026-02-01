using UnityEngine;

// Per-creep simulation state. Position and health mutated by owning systems; Id is immutable.
public sealed class CreepSimData
{
    public int Id { get; }
    public Vector3 Position;
    public Vector3 Target;
    public float Speed;
    public bool ReachedBase;
    public int DamageToBase;
    public bool HasDealtBaseDamage;
    public int Health;
    public int MaxHealth;
    public int CoinReward;

    public CreepSimData(int id)
    {
        Id = id;
    }
}
