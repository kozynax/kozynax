using System;
using System.Collections.Generic;

namespace ZXMAK2.Dependency
{
    /// <summary>
    /// Lightweight DI container used by both hosts (no external IoC library).
    /// </summary>
    public sealed class ResolverSimple : IResolver
    {
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private bool _isDisposed;

        public ResolverSimple()
        {
            RegisterInstance<IResolver>(this);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            foreach (var pair in _singletons)
            {
                (pair.Value as IDisposable)?.Dispose();
            }
            _singletons.Clear();
            _factories.Clear();
        }

        public T Resolve<T>()
        {
            var type = typeof(T);
            if (_singletons.TryGetValue(type, out var singleton))
                return (T)singleton;

            if (_factories.TryGetValue(type, out var factory))
            {
                var instance = factory();
                return (T)instance;
            }

            throw new InvalidOperationException($"Type not registered: {type.FullName}");
        }

        public T TryResolve<T>()
        {
            try
            {
                if (!CheckAvailable<T>())
                    return default(T);
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
            var type = typeof(T);
            return _singletons.ContainsKey(type) || _factories.ContainsKey(type);
        }

        public void RegisterInstance<T>(T instance)
        {
            _singletons[typeof(T)] = instance;
        }

        public void RegisterType<TBase, TConcrete>(bool singleton = false)
            where TConcrete : TBase
        {
            if (singleton)
            {
                _factories[typeof(TBase)] = () =>
                {
                    if (_singletons.TryGetValue(typeof(TBase), out var existing))
                        return existing;
                    var created = CreateInstance(typeof(TConcrete));
                    _singletons[typeof(TBase)] = created;
                    return created;
                };
            }
            else
            {
                _factories[typeof(TBase)] = () => CreateInstance(typeof(TConcrete));
            }
        }

        private object CreateInstance(Type type)
        {
            var ctors = type.GetConstructors();
            if (ctors.Length == 0)
                return Activator.CreateInstance(type);

            var ctor = ctors[0];
            foreach (var candidate in ctors)
            {
                if (candidate.GetParameters().Length > ctor.GetParameters().Length)
                    ctor = candidate;
            }

            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var resolve = typeof(ResolverSimple).GetMethod(nameof(Resolve))
                    .MakeGenericMethod(paramType);
                args[i] = resolve.Invoke(this, null);
            }
            return ctor.Invoke(args);
        }
    }
}
