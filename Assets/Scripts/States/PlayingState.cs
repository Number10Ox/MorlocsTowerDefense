using System;

// Polls BaseStore.IsDestroyed each tick; fires BaseDestroyed once when detected. Resets guard on Enter.
public class PlayingState : IGameState
{
    private readonly Action<GameTrigger> fire;
    private readonly BaseStore baseStore;
    private bool baseDestroyedFired;

    public PlayingState(Action<GameTrigger> fire, BaseStore baseStore)
    {
        this.fire = fire ?? throw new ArgumentNullException(nameof(fire));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
    }

    public void Enter()
    {
        baseDestroyedFired = false;
    }

    public void Tick(float deltaTime)
    {
        if (!baseDestroyedFired && baseStore.IsDestroyed)
        {
            baseDestroyedFired = true;
            fire(GameTrigger.BaseDestroyed);
        }
    }

    public void Exit()
    {
    }
}
