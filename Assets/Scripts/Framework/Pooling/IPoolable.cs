namespace ObjectPooling
{
    // Lifecycle callbacks for pooled GameObjects: activate on get, reset and deactivate on return.
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolReturn();
    }
}
