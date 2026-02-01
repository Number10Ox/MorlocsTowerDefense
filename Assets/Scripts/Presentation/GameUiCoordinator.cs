using System;
using UnityEngine;

// State-driven presentation decisions: popup lifecycle, HUD visibility, health forwarding.
// Subscribes to state machine and store events. No simulation writes.
public class GameUiCoordinator
{
    private GameStateMachine stateMachine;
    private BaseStore baseStore;
    private BaseHealthHud baseHealthHud;
    private readonly GameObject losePopupPrefab;
    private readonly Transform popupParent;
    private GameObject losePopupInstance;
    private bool tornDown;

    public GameUiCoordinator(
        GameStateMachine stateMachine,
        BaseStore baseStore,
        BaseHealthHud baseHealthHud,
        GameObject losePopupPrefab,
        Transform popupParent)
    {
        this.stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
        this.baseHealthHud = baseHealthHud;
        this.losePopupPrefab = losePopupPrefab;
        this.popupParent = popupParent;

        stateMachine.OnStateChanged += OnStateChanged;

        if (baseHealthHud != null)
        {
            baseStore.OnBaseHealthChanged += OnBaseHealthChanged;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (baseHealthHud != null)
        {
            baseHealthHud.UpdateHealth(baseStore.CurrentHealth, baseStore.MaxHealth);
            baseHealthHud.SetVisible(stateMachine.CurrentStateId == GameState.Playing);
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

        if (baseHealthHud != null)
        {
            baseHealthHud.SetVisible(to == GameState.Playing);
        }
    }

    private void OnBaseHealthChanged(int current, int max)
    {
        if (baseHealthHud != null)
        {
            baseHealthHud.UpdateHealth(current, max);
        }
    }
}
