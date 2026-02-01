using System;

// Consumes PlacementInput, creates turret entities in TurretStore. Clears input after consuming.
public class PlacementSystem : IGameSystem
{
    private readonly TurretStore turretStore;
    private readonly PlacementInput placementInput;
    private readonly float turretRange;
    private readonly float turretFireInterval;
    private readonly int turretDamage;
    private readonly float turretProjectileSpeed;
    private int nextTurretId;

    public PlacementSystem(
        TurretStore turretStore,
        PlacementInput placementInput,
        float turretRange,
        float turretFireInterval,
        int turretDamage,
        float turretProjectileSpeed)
    {
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.placementInput = placementInput ?? throw new ArgumentNullException(nameof(placementInput));
        this.turretRange = turretRange;
        this.turretFireInterval = turretFireInterval;
        this.turretDamage = turretDamage;
        this.turretProjectileSpeed = turretProjectileSpeed;
    }

    public void Tick(float deltaTime)
    {
        if (!placementInput.PlaceRequested) return;

        var turret = new TurretSimData(nextTurretId++)
        {
            Position = placementInput.WorldPosition,
            Range = turretRange,
            FireInterval = turretFireInterval,
            Damage = turretDamage,
            ProjectileSpeed = turretProjectileSpeed
        };

        turretStore.Add(turret);
        placementInput.Clear();
    }

    public void Reset()
    {
        nextTurretId = 0;
    }
}
