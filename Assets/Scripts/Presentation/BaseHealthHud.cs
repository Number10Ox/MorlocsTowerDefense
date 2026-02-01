using UnityEngine.UIElements;

// Binds to UI Toolkit elements for health display. Stateless view: caller provides current/max.
public class BaseHealthHud
{
    private const string HEALTH_LABEL_NAME = "health-label";
    private const string HEALTH_BAR_FILL_NAME = "health-bar-fill";
    private const string HEALTH_CONTAINER_NAME = "health-container";

    private readonly Label healthLabel;
    private readonly VisualElement healthBarFill;
    private readonly VisualElement healthContainer;

    public BaseHealthHud(UIDocument uiDocument)
    {
        var root = uiDocument.rootVisualElement;
        healthLabel = root.Q<Label>(HEALTH_LABEL_NAME);
        healthBarFill = root.Q<VisualElement>(HEALTH_BAR_FILL_NAME);
        healthContainer = root.Q<VisualElement>(HEALTH_CONTAINER_NAME);
    }

    public void UpdateHealth(int current, int max)
    {
        if (healthLabel != null)
        {
            healthLabel.text = $"{current} / {max}";
        }

        if (healthBarFill != null && max > 0)
        {
            float percent = (float)current / max * 100f;
            healthBarFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
        }
    }

    public void SetVisible(bool visible)
    {
        if (healthContainer != null)
        {
            healthContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
