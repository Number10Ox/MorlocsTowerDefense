using System;

public class PlacementSystem : IGameSystem
{
    private readonly TurretStore turretStore;
    private readonly PlacementInput placementInput;
    private int nextTurretId;

    public PlacementSystem(TurretStore turretStore, PlacementInput placementInput)
    {
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.placementInput = placementInput ?? throw new ArgumentNullException(nameof(placementInput));
    }

    public void Tick(float deltaTime)
    {
        if (!placementInput.PlaceRequested) return;

        var turret = new TurretSimData(nextTurretId++)
        {
            Position = placementInput.WorldPosition
        };

        turretStore.Add(turret);
        placementInput.Clear();
    }

    public void Reset()
    {
        nextTurretId = 0;
    }
}
