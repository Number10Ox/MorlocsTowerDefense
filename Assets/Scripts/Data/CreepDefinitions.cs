using UnityEngine;

// Single asset containing all creep type definitions. Authored in Inspector; consumed at bootstrap.
// Ordering contract: array order determines round-robin spawn cycling.
[CreateAssetMenu(fileName = "CreepDefinitions", menuName = "Game/Creep Definitions")]
public class CreepDefinitions : ScriptableObject
{
    [Tooltip("Ordered creep definitions. Order determines round-robin spawn cycling.")]
    [SerializeField] private CreepTypeDefinition[] entries;

    public CreepTypeDefinition[] Entries => entries;

    private void OnValidate()
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            var def = entries[i];
            def.Validate();
            entries[i] = def;
        }
    }
}
