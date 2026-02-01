using System.Collections.Generic;
using UnityEngine;

// Single asset containing all turret type definitions. Authored in Inspector; consumed at bootstrap.
// Ordering contract: first entry = default type; array order determines keyboard shortcuts (1-9) and HUD layout.
[CreateAssetMenu(fileName = "TurretDefinitions", menuName = "Game/Turret Definitions")]
public class TurretDefinitions : ScriptableObject
{
    [Tooltip("Ordered turret definitions. First entry = default type. Order determines keyboard shortcuts (1-9) and HUD layout.")]
    [SerializeField] private TurretTypeDefinition[] entries;

    public TurretTypeDefinition[] Entries => entries;

    private void OnValidate()
    {
        if (entries == null) return;
        var seenTypes = new HashSet<TurretType>();
        for (int i = 0; i < entries.Length; i++)
        {
            var def = entries[i];
            def.Validate();
            entries[i] = def;

            if (!seenTypes.Add(def.TurretType) && def.TurretType != default)
            {
                Debug.LogWarning($"TurretDefinitions: Duplicate turret type {def.TurretType} at entries[{i}].");
            }
        }
    }
}
