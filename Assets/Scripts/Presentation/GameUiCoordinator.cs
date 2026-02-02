using System;
using UnityEngine;

// State-driven presentation decisions: popup lifecycle, HUD visibility, health/coin/turret-selection forwarding.
// Subscribes to state machine, store events, and turret selection events. No simulation writes.
public class GameUiCoordinator
{
    private GameStateMachine stateMachine;
    private BaseStore baseStore;
    private EconomyStore economyStore;
    private TurretSelectionStore selectionStore;
    private BaseHealthHud baseHealthHud;
    private CoinHud coinHud;
    private TurretSelectionHud turretSelectionHud;
    private readonly GameObject losePopupPrefab;
    private readonly GameObject winPopupPrefab;
    private readonly Transform popupParent;
    private GameObject losePopupInstance;
    private GameObject winPopupInstance;
    private bool tornDown;

    public GameUiCoordinator(
        GameStateMachine stateMachine,
        BaseStore baseStore,
        EconomyStore economyStore,
        TurretSelectionStore selectionStore,
        BaseHealthHud baseHealthHud,
        CoinHud coinHud,
        TurretSelectionHud turretSelectionHud,
        GameObject losePopupPrefab,
        GameObject winPopupPrefab,
        Transform popupParent)
    {
        this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
        this.economyStore = economyStore ?? throw new ArgumentNullException(nameof(economyStore));
        this.selectionStore = selectionStore;
        this.baseHealthHud = baseHealthHud;
        this.coinHud = coinHud;
        this.turretSelectionHud = turretSelectionHud;
        this.losePopupPrefab = losePopupPrefab;
        this.winPopupPrefab = winPopupPrefab;
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

        if (turretSelectionHud != null && selectionStore != null)
        {
            selectionStore.OnSelectionChanged += OnTurretSelectionChanged;
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

        if (turretSelectionHud != null)
        {
            if (selectionStore != null)
            {
                turretSelectionHud.UpdateSelection(selectionStore.SelectedType);
            }
            turretSelectionHud.SetVisible(isPlaying);
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

        if (selectionStore != null && turretSelectionHud != null)
        {
            selectionStore.OnSelectionChanged -= OnTurretSelectionChanged;
        }
        selectionStore = null;
        turretSelectionHud = null;

        if (losePopupInstance != null)
        {
            UnityEngine.Object.Destroy(losePopupInstance);
            losePopupInstance = null;
        }

        if (winPopupInstance != null)
        {
            UnityEngine.Object.Destroy(winPopupInstance);
            winPopupInstance = null;
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

        if (to == GameState.Win && winPopupPrefab != null)
        {
            winPopupInstance = popupParent != null
                ? UnityEngine.Object.Instantiate(winPopupPrefab, popupParent)
                : UnityEngine.Object.Instantiate(winPopupPrefab);
        }

        if (from == GameState.Win && winPopupInstance != null)
        {
            UnityEngine.Object.Destroy(winPopupInstance);
            winPopupInstance = null;
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

        if (turretSelectionHud != null)
        {
            turretSelectionHud.SetVisible(isPlaying);
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

    private void OnTurretSelectionChanged(TurretType type)
    {
        if (turretSelectionHud != null)
        {
            turretSelectionHud.UpdateSelection(type);
        }
    }
}
