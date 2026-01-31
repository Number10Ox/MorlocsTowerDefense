using System;

public class PlayingState : IGameState
{
    private readonly Action<GameTrigger> fire;

    public PlayingState(Action<GameTrigger> fire)
    {
        this.fire = fire;
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
