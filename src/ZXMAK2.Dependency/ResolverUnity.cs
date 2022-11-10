using System;
using System.Linq;
using Microsoft.Practices.Unity;

namespace ZXMAK2.Dependency
{
    public sealed class ResolverUnity : IResolver
    {
        private readonly IUnityContainer _container;
        private bool _isDisposed;
        
        public ResolverUnity()
        {
            _container = new UnityContainer();
            _container.RegisterInstance<IResolver>(this);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                // we registered in contaner, 
                // so we need to prevent reentrancy
                return;
            }
            _isDisposed = true;
            _container.Dispose();
        }

        public T Resolve<T>() => _container.Resolve<T>();

        public T TryResolve<T>()
        {
            try
            {
                if (!CheckAvailable<T>())
                {
                    return default(T);
                }
                return Resolve<T>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return default(T);
            }
        }

        public bool CheckAvailable<T>()
        {
            return _container.IsRegistered<T>();
        }

        public void RegisterInstance<T>(T instance)
        {
            _container.RegisterInstance<T>(instance);
        }

        public void RegisterType<TBase, TConcrete>(bool singleton = false) where TConcrete : TBase
        {
            if (singleton)
                _container.RegisterType<TBase, TConcrete>(new TransientLifetimeManager());
            else                
                _container.RegisterType<TBase, TConcrete>();
        }
    }
}
