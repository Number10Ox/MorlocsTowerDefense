using System;

// Win game state. Presentation (popup, HUD) handled by GameUiCoordinator.
public class WinState : IGameState
{
    private readonly Action<GameTrigger> fire;

    public WinState(Action<GameTrigger> fire)
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
