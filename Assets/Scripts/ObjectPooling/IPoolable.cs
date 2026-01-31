namespace ObjectPooling
{
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolReturn();
    }
}
