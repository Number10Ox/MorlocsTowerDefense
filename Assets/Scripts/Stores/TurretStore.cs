using System;
using System.Collections.Generic;

// Authoritative turret collection. Writer: PlacementSystem (Add). No removal pipeline yet.
public class TurretStore
{
    private readonly List<TurretSimData> activeTurrets = new();
    private readonly List<TurretSimData> placedThisFrame = new();

    public IReadOnlyList<TurretSimData> ActiveTurrets => activeTurrets;
    public IReadOnlyList<TurretSimData> PlacedThisFrame => placedThisFrame;

    public void BeginFrame()
    {
        placedThisFrame.Clear();
    }

    public void Add(TurretSimData turret)
    {
        if (turret == null)
        {
            throw new ArgumentNullException(nameof(turret));
        }

        activeTurrets.Add(turret);
        placedThisFrame.Add(turret);
    }

    public void Reset()
    {
        activeTurrets.Clear();
        placedThisFrame.Clear();
    }
}
