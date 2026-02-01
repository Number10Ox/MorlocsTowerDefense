using System;

// Consumes PlacementInput, gates on economy affordability, creates turret entities in TurretStore.
// Clears input after consuming. Reads EconomyStore (never writes).
public class PlacementSystem : IGameSystem
{
    private readonly TurretStore turretStore;
    private readonly PlacementInput placementInput;
    private readonly EconomyStore economyStore;
    private readonly float turretRange;
    private readonly float turretFireInterval;
    private readonly int turretDamage;
    private readonly float turretProjectileSpeed;
    private readonly int turretCost;
    private int nextTurretId;

    public PlacementSystem(
        TurretStore turretStore,
        PlacementInput placementInput,
        EconomyStore economyStore,
        float turretRange,
        float turretFireInterval,
        int turretDamage,
        float turretProjectileSpeed,
        int turretCost)
    {
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.placementInput = placementInput ?? throw new ArgumentNullException(nameof(placementInput));
        this.economyStore = economyStore ?? throw new ArgumentNullException(nameof(economyStore));
        this.turretRange = turretRange;
        this.turretFireInterval = turretFireInterval;
        this.turretDamage = turretDamage;
        this.turretProjectileSpeed = turretProjectileSpeed;
        this.turretCost = turretCost;
    }

    public void Tick(float deltaTime)
    {
        if (!placementInput.PlaceRequested) return;

        if (!economyStore.CanAfford(turretCost))
        {
            placementInput.Clear();
            return;
        }

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
