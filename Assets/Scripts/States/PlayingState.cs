using System;

// Polls end conditions each tick: BaseDestroyed (lose, priority), AllWavesCleared (win).
// Fires triggers with one-shot guards. Resets guards on Enter.
public class PlayingState : IGameState
{
    private readonly Action<GameTrigger> fire;
    private readonly BaseStore baseStore;
    private readonly WaveStore waveStore;
    private bool baseDestroyedFired;
    private bool allWavesClearedFired;

    public PlayingState(Action<GameTrigger> fire, BaseStore baseStore, WaveStore waveStore)
    {
        this.fire = fire ?? throw new ArgumentNullException(nameof(fire));
        this.baseStore = baseStore ?? throw new ArgumentNullException(nameof(baseStore));
        this.waveStore = waveStore ?? throw new ArgumentNullException(nameof(waveStore));
    }

    public void Enter()
    {
        baseDestroyedFired = false;
        allWavesClearedFired = false;
    }

    public void Tick(float deltaTime)
    {
        if (baseDestroyedFired || allWavesClearedFired)
        {
            return;
        }

        if (baseStore.IsDestroyed)
        {
            baseDestroyedFired = true;
            fire(GameTrigger.BaseDestroyed);
            return;
        }

        if (waveStore.AllWavesCleared)
        {
            allWavesClearedFired = true;
            fire(GameTrigger.AllWavesCleared);
        }
    }

    public void Exit()
    {
    }
}
