using System.Collections.Generic;
using UnityEngine;

// Pure C# helper: validates TurretDefinitions entries and builds a TurretTypeDirectory.
// Keeps GameFlowController.Awake() clean and makes definitions validation unit-testable.
// Locally copies and validates each entry so runtime correctness never depends on OnValidate.
public static class TurretTypeDirectoryBuilder
{
    public static bool TryBuild(
        TurretTypeDefinition[] entries,
        out TurretTypeDirectory directory,
        out string error)
    {
        directory = null;
        error = null;

        if (entries == null || entries.Length == 0)
        {
            error = "TurretDefinitions has no entries.";
            return false;
        }

        var statsByType = new Dictionary<TurretType, TurretTypeStats>(entries.Length);
        var prefabsByType = new Dictionary<TurretType, GameObject>(entries.Length);
        var orderedStats = new TurretTypeStats[entries.Length];
        var orderedTypes = new TurretType[entries.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            var def = entries[i];
            def.Validate();

            TurretType type = def.TurretType;

            if (statsByType.ContainsKey(type))
            {
                error = $"Duplicate turret type: {type} (entries[{i}]).";
                return false;
            }

            if (def.Prefab == null)
            {
                error = $"Null prefab for {type} (entries[{i}]).";
                return false;
            }

            WarnSuspiciousSlowConfig(def, i);

            var stats = new TurretTypeStats(
                type,
                def.Range,
                def.FireInterval,
                def.Damage,
                def.ProjectileSpeed,
                def.Cost,
                def.SlowDuration,
                def.SlowMultiplier);

            statsByType[type] = stats;
            prefabsByType[type] = def.Prefab;
            orderedStats[i] = stats;
            orderedTypes[i] = type;
        }

        directory = new TurretTypeDirectory(orderedTypes, orderedStats, statsByType, prefabsByType);
        return true;
    }

    private static void WarnSuspiciousSlowConfig(TurretTypeDefinition def, int index)
    {
        if (def.SlowDuration == 0f && def.SlowMultiplier != 1f)
        {
            Debug.LogWarning(
                $"TurretDefinitions: {def.TurretType} (entries[{index}]) has slowDuration=0 but slowMultiplier={def.SlowMultiplier}. " +
                "Slow multiplier has no effect without duration.");
        }
        else if (def.SlowDuration > 0f && def.SlowMultiplier == 1f)
        {
            Debug.LogWarning(
                $"TurretDefinitions: {def.TurretType} (entries[{index}]) has slowDuration={def.SlowDuration} but slowMultiplier=1. " +
                "Slow duration has no visible effect at 100% speed.");
        }
    }
}
