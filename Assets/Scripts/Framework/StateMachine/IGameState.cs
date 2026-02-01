// Lifecycle contract for game states: Enter, Tick, Exit.
public interface IGameState
{
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}
