using UnityEngine;

public sealed class TurretSimData
{
    public int Id { get; }
    public Vector3 Position;

    public TurretSimData(int id)
    {
        Id = id;
    }
}
