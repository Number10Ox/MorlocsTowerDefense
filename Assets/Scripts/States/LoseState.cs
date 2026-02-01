using System;

// Lose game state. Presentation (popup, HUD) handled by GameUiCoordinator.
public class LoseState : IGameState
{
    private readonly Action<GameTrigger> fire;

    public LoseState(Action<GameTrigger> fire)
    {
        this.fire = fire ?? throw new ArgumentNullException(nameof(fire));
    }

    public void Enter()
    {
    }

    public void Tick(float deltaTime)
    {
    }

    public void Exit()
    {
    }
}
