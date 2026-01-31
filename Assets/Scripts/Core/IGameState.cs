public interface IGameState
{
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}
