using System;
using System.Collections.Generic;
using UnityEngine;

// Consumes PlacementInput, gates on economy affordability, creates typed turret entities in TurretStore.
// Reads TurretSelectionStore for selected type, looks up matching TurretTypeStats from dictionary.
// Clears input after consuming. Reads EconomyStore (never writes).
public class PlacementSystem : IGameSystem
{
    private readonly TurretStore turretStore;
    private readonly PlacementInput placementInput;
    private readonly EconomyStore economyStore;
    private readonly TurretSelectionStore selectionStore;
    private readonly IReadOnlyDictionary<TurretType, TurretTypeStats> statsByType;
    private int nextTurretId;

    public PlacementSystem(
        TurretStore turretStore,
        PlacementInput placementInput,
        EconomyStore economyStore,
        TurretSelectionStore selectionStore,
        IReadOnlyDictionary<TurretType, TurretTypeStats> statsByType)
    {
        this.turretStore = turretStore ?? throw new ArgumentNullException(nameof(turretStore));
        this.placementInput = placementInput ?? throw new ArgumentNullException(nameof(placementInput));
        this.economyStore = economyStore ?? throw new ArgumentNullException(nameof(economyStore));
        this.selectionStore = selectionStore ?? throw new ArgumentNullException(nameof(selectionStore));
        this.statsByType = statsByType ?? throw new ArgumentNullException(nameof(statsByType));
    }

    public void Tick(float deltaTime)
    {
        if (!placementInput.PlaceRequested) return;

        TurretType selectedType = selectionStore.SelectedType;
        if (!statsByType.TryGetValue(selectedType, out TurretTypeStats stats))
        {
            Debug.LogWarning($"PlacementSystem: No stats for TurretType {selectedType}.");
            placementInput.Clear();
            return;
        }

        if (!economyStore.CanAfford(stats.Cost))
        {
            placementInput.Clear();
            return;
        }

        var turret = new TurretSimData(nextTurretId++)
        {
            Type = selectedType,
            Position = placementInput.WorldPosition,
            Range = stats.Range,
            FireInterval = stats.FireInterval,
            Damage = stats.Damage,
            ProjectileSpeed = stats.ProjectileSpeed,
            SlowDuration = stats.SlowDuration,
            SlowMultiplier = stats.SlowMultiplier
        };

        turretStore.Add(turret);
        placementInput.Clear();
    }

    public void Reset()
    {
        nextTurretId = 0;
    }
}
