using System.Collections.Generic;
using UnityEngine;

// Pure C# helper: validates CreepDefinitions entries and builds a CreepTypeDirectory.
// Keeps GameFlowController.Awake() clean and makes definitions validation unit-testable.
// Locally copies and validates each entry so runtime correctness never depends on OnValidate.
public static class CreepTypeDirectoryBuilder
{
    public static bool TryBuild(
        CreepTypeDefinition[] entries,
        out CreepTypeDirectory directory,
        out string error)
    {
        directory = null;
        error = null;

        if (entries == null || entries.Length == 0)
        {
            error = "CreepDefinitions has no entries.";
            return false;
        }

        var statsByType = new Dictionary<CreepType, CreepTypeStats>(entries.Length);
        var prefabsByType = new Dictionary<CreepType, GameObject>(entries.Length);
        var orderedStats = new CreepTypeStats[entries.Length];
        var orderedTypes = new CreepType[entries.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            var def = entries[i];
            def.Validate();

            CreepType type = def.CreepType;

            if (statsByType.ContainsKey(type))
            {
                error = $"Duplicate creep type: {type} (entries[{i}]).";
                return false;
            }

            if (def.Prefab == null)
            {
                error = $"Null prefab for {type} (entries[{i}]).";
                return false;
            }

            var stats = new CreepTypeStats(
                type,
                def.Speed,
                def.DamageToBase,
                def.MaxHealth,
                def.CoinReward);

            statsByType[type] = stats;
            prefabsByType[type] = def.Prefab;
            orderedStats[i] = stats;
            orderedTypes[i] = type;
        }

        directory = new CreepTypeDirectory(orderedTypes, orderedStats, statsByType, prefabsByType);
        return true;
    }
}
