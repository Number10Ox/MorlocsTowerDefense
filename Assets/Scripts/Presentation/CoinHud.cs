using UnityEngine;
using UnityEngine.UIElements;

// Binds to UI Toolkit elements for coin display. Stateless view: caller provides current balance.
public class CoinHud
{
    private const string COIN_LABEL_NAME = "coin-label";
    private const string COIN_CONTAINER_NAME = "coin-container";

    private readonly Label coinLabel;
    private readonly VisualElement coinContainer;

    public CoinHud(UIDocument uiDocument)
    {
        var root = uiDocument.rootVisualElement;
        coinLabel = root.Q<Label>(COIN_LABEL_NAME);
        coinContainer = root.Q<VisualElement>(COIN_CONTAINER_NAME);

        if (coinLabel == null)
        {
            Debug.LogWarning("CoinHud: coin-label element not found in UIDocument.");
        }

        if (coinContainer == null)
        {
            Debug.LogWarning("CoinHud: coin-container element not found in UIDocument.");
        }
    }

    public void UpdateCoins(int amount)
    {
        if (coinLabel != null)
        {
            coinLabel.text = $"Coins: {amount}";
        }
    }

    public void SetVisible(bool visible)
    {
        if (coinContainer != null)
        {
            coinContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
