using System;


namespace ZXMAK2.Dependency
{
    public interface IResolver : IDisposable
    {
        T Resolve<T>();
        T TryResolve<T>();
        bool CheckAvailable<T>();
    }
}
