using System;
using UnityEngine;

// Validates scene setup on Enter, fires SceneValidated to advance to Playing.
public class InitState : IGameState
{
    private readonly Action<GameTrigger> fire;
    private readonly HomeBaseComponent homeBase;

    public InitState(Action<GameTrigger> fire, HomeBaseComponent homeBase)
    {
        this.fire = fire ?? throw new ArgumentNullException(nameof(fire));
        this.homeBase = homeBase;
    }

    public void Enter()
    {
        if (homeBase == null)
        {
            Debug.LogError("InitState: HomeBaseComponent reference is null. Scene setup is invalid.");
            return;
        }

        fire(GameTrigger.SceneValidated);
    }

    public void Tick(float deltaTime)
    {
    }

    public void Exit()
    {
    }
}
