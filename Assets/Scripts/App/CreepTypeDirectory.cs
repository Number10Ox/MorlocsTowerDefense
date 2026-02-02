using System.Collections.Generic;
using UnityEngine;

// Immutable runtime directory built from CreepDefinitions entries.
// Holds all per-type lookups (stats, prefabs) and ordered arrays for spawn cycling.
public sealed class CreepTypeDirectory
{
    public CreepType[] OrderedTypes { get; }
    public CreepTypeStats[] OrderedStats { get; }
    public IReadOnlyDictionary<CreepType, CreepTypeStats> StatsByType { get; }
    public IReadOnlyDictionary<CreepType, GameObject> PrefabsByType { get; }

    public CreepTypeDirectory(
        CreepType[] orderedTypes,
        CreepTypeStats[] orderedStats,
        Dictionary<CreepType, CreepTypeStats> statsByType,
        Dictionary<CreepType, GameObject> prefabsByType)
    {
        OrderedTypes = (CreepType[])orderedTypes.Clone();
        OrderedStats = (CreepTypeStats[])orderedStats.Clone();
        StatsByType = statsByType;
        PrefabsByType = prefabsByType;
    }
}
