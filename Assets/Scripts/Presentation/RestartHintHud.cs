using UnityEngine;
using UnityEngine.UIElements;

// Binds to UI Toolkit elements for restart hint display. Shows "Press R to Restart" in Win/Lose states.
public class RestartHintHud
{
    private const string RESTART_HINT_CONTAINER_NAME = "restart-hint-container";
    private readonly VisualElement restartHintContainer;

    public RestartHintHud(UIDocument uiDocument)
    {
        var root = uiDocument.rootVisualElement;
        restartHintContainer = root.Q<VisualElement>(RESTART_HINT_CONTAINER_NAME);

        if (restartHintContainer == null)
        {
            Debug.LogWarning("RestartHintHud: restart-hint-container element not found in UIDocument.");
        }
    }

    public void SetVisible(bool visible)
    {
        if (restartHintContainer != null)
        {
            restartHintContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
