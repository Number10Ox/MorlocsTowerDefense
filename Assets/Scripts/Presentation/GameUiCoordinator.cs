using System;
using UnityEngine;

// State-driven presentation decisions: popup lifecycle, HUD visibility, health and coin forwarding.
// Subscribes to state machine and store events. No simulation writes.
public class GameUiCoordinator
{
    private GameStateMachine stateMachine;
    private BaseStore baseStore;
    private EconomyStore economyStore;
    private BaseHealthHud baseHealthHud;
    private CoinHud coinHud;
    private readonly GameObject losePopupPrefab;
    private readonly Transform popupParent;
    private GameObject losePopupInstance;
    private bool tornDown;

    public GameUiCoordinator(
        GameStateMachine stateMachine,
        BaseStore baseStore,
        EconomyStore economyStore,
        BaseHealthHud baseHealthHud,
        CoinHud coinHud,
        GameObject losePopupPrefab,
        Transform popupParent)
    {
        this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
        this.economyStore = economyStore ?? throw new ArgumentNullException(nameof(economyStore));
        this.baseHealthHud = baseHealthHud;
        this.coinHud = coinHud;
        this.losePopupPrefab = losePopupPrefab;
        this.popupParent = popupParent;

        stateMachine.OnStateChanged += OnStateChanged;

        if (baseHealthHud != null)
        {
            baseStore.OnBaseHealthChanged += OnBaseHealthChanged;
        }

        if (coinHud != null)
        {
            economyStore.OnCoinsChanged += OnCoinsChanged;
        }

        Refresh();
    }

    public void Refresh()
    {
        bool isPlaying = stateMachine.CurrentStateId == GameState.Playing;

        if (baseHealthHud != null)
        {
            baseHealthHud.UpdateHealth(baseStore.CurrentHealth, baseStore.MaxHealth);
            baseHealthHud.SetVisible(isPlaying);
        }

        if (coinHud != null)
        {
            coinHud.UpdateCoins(economyStore.CurrentCoins);
            coinHud.SetVisible(isPlaying);
        }
    }

    public void Teardown()
    {
        if (tornDown) return;
        tornDown = true;

        if (stateMachine != null)
        {
            stateMachine.OnStateChanged -= OnStateChanged;
            stateMachine = null;
        }

        if (baseStore != null && baseHealthHud != null)
        {
            baseStore.OnBaseHealthChanged -= OnBaseHealthChanged;
        }
        baseStore = null;
        baseHealthHud = null;

        if (economyStore != null && coinHud != null)
        {
            economyStore.OnCoinsChanged -= OnCoinsChanged;
        }
        economyStore = null;
        coinHud = null;

        if (losePopupInstance != null)
        {
            UnityEngine.Object.Destroy(losePopupInstance);
            losePopupInstance = null;
        }
    }

    private void OnStateChanged(GameState from, GameState to)
    {
        Debug.Log($"State changed: {from} -> {to}");

        if (to == GameState.Lose && losePopupPrefab != null)
        {
            losePopupInstance = popupParent != null
                ? UnityEngine.Object.Instantiate(losePopupPrefab, popupParent)
                : UnityEngine.Object.Instantiate(losePopupPrefab);
        }

        if (from == GameState.Lose && losePopupInstance != null)
        {
            UnityEngine.Object.Destroy(losePopupInstance);
            losePopupInstance = null;
        }

        bool isPlaying = to == GameState.Playing;

        if (baseHealthHud != null)
        {
            baseHealthHud.SetVisible(isPlaying);
        }

        if (coinHud != null)
        {
            coinHud.SetVisible(isPlaying);
        }
    }

    private void OnBaseHealthChanged(int current, int max)
    {
        if (baseHealthHud != null)
        {
            baseHealthHud.UpdateHealth(current, max);
        }
    }

    private void OnCoinsChanged(int currentCoins)
    {
        if (coinHud != null)
        {
            coinHud.UpdateCoins(currentCoins);
        }
    }
}
