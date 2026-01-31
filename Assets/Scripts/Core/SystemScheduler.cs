public class SystemScheduler
{
    private readonly IGameSystem[] systems;

    public SystemScheduler(IGameSystem[] systems)
    {
        this.systems = systems;
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Tick(deltaTime);
        }
    }
}
