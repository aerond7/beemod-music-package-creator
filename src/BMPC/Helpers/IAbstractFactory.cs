namespace BMPC.Helpers
{
    public interface IAbstractFactory<T>
    {
        T Create();
    }
}