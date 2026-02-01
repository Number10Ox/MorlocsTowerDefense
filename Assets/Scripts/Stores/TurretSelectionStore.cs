using System;

// Authoritative owner of the currently selected turret type.
// Writer: PresentationAdapter (CollectInput, keyboard). Readers: PlacementSystem, GameUiCoordinator.
public class TurretSelectionStore
{
    private readonly TurretType defaultType;
    private TurretType selectedType;

    public TurretType SelectedType => selectedType;

    public event Action<TurretType> OnSelectionChanged;

    public TurretSelectionStore(TurretType defaultType)
    {
        this.defaultType = defaultType;
        selectedType = defaultType;
    }

    public void SelectType(TurretType type)
    {
        if (selectedType == type) return;

        selectedType = type;
        OnSelectionChanged?.Invoke(selectedType);
    }

    public void Reset() => SelectType(defaultType);
}
