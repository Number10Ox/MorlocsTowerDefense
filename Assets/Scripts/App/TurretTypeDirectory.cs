using System.Collections.Generic;
using UnityEngine;

// Immutable runtime directory built from TurretDefinitions entries.
// Holds all per-type lookups (stats, prefabs) and ordered arrays for HUD/keyboard mapping.
public sealed class TurretTypeDirectory
{
    public TurretType[] OrderedTypes { get; }
    public TurretTypeStats[] OrderedStats { get; }
    public IReadOnlyDictionary<TurretType, TurretTypeStats> StatsByType { get; }
    public IReadOnlyDictionary<TurretType, GameObject> PrefabsByType { get; }

    public TurretType DefaultType => OrderedTypes[0];

    public TurretTypeDirectory(
        TurretType[] orderedTypes,
        TurretTypeStats[] orderedStats,
        Dictionary<TurretType, TurretTypeStats> statsByType,
        Dictionary<TurretType, GameObject> prefabsByType)
    {
        OrderedTypes = (TurretType[])orderedTypes.Clone();
        OrderedStats = (TurretTypeStats[])orderedStats.Clone();
        StatsByType = statsByType;
        PrefabsByType = prefabsByType;
    }
}
