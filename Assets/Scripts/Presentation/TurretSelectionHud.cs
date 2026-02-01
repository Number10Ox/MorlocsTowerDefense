using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Dynamically generates turret selection UI elements from TurretTypeStats[].
// Stateless view: caller provides selected type. Keyboard shortcuts shown for first 9 entries.
public class TurretSelectionHud
{
    private const string CONTAINER_NAME = "turret-selection-container";
    private const string OPTIONS_CONTAINER_NAME = "turret-options";
    private const string SELECTED_CLASS = "turret-option--selected";
    private const int MAX_KEYBOARD_SHORTCUTS = 9;

    private readonly VisualElement container;
    private readonly Dictionary<TurretType, VisualElement> optionElements;
    private TurretType currentSelectedType;

    public TurretSelectionHud(UIDocument uiDocument, TurretTypeStats[] turretTypes, TurretType initialSelection)
    {
        var root = uiDocument.rootVisualElement;
        container = root.Q<VisualElement>(CONTAINER_NAME);
        optionElements = new Dictionary<TurretType, VisualElement>(turretTypes.Length);

        if (container == null)
        {
            Debug.LogWarning("TurretSelectionHud: turret-selection-container element not found in UIDocument.");
            return;
        }

        var optionsContainer = container.Q<VisualElement>(OPTIONS_CONTAINER_NAME);
        if (optionsContainer == null)
        {
            Debug.LogWarning("TurretSelectionHud: turret-options element not found in UIDocument.");
            return;
        }

        optionsContainer.Clear();

        for (int i = 0; i < turretTypes.Length; i++)
        {
            var stats = turretTypes[i];
            var option = new VisualElement();
            option.AddToClassList("turret-option");

            string shortcut = i < MAX_KEYBOARD_SHORTCUTS ? $"[{i + 1}] " : "";
            var label = new Label($"{shortcut}{stats.Type} (${stats.Cost})");
            label.AddToClassList("turret-option-label");
            option.Add(label);

            optionsContainer.Add(option);
            optionElements[stats.Type] = option;
        }

        // Apply initial selection directly — can't use UpdateSelection because
        // currentSelectedType defaults to enum value 0 which may match initialSelection.
        currentSelectedType = initialSelection;
        if (optionElements.TryGetValue(initialSelection, out var initialOption))
        {
            initialOption.AddToClassList(SELECTED_CLASS);
        }
    }

    public void UpdateSelection(TurretType type)
    {
        if (type == currentSelectedType) return;

        if (optionElements.TryGetValue(currentSelectedType, out var oldOption))
        {
            oldOption.RemoveFromClassList(SELECTED_CLASS);
        }

        if (optionElements.TryGetValue(type, out var newOption))
        {
            newOption.AddToClassList(SELECTED_CLASS);
        }

        currentSelectedType = type;
    }

    public void SetVisible(bool visible)
    {
        if (container != null)
        {
            container.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
