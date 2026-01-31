using UnityEngine;

public sealed class CreepSimData
{
    public int Id { get; }
    public Vector3 Position;
    public Vector3 Target;
    public float Speed;
    public bool ReachedBase;

    public CreepSimData(int id)
    {
        Id = id;
    }
}
