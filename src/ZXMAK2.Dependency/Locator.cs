

namespace ZXMAK2.Dependency
{
    public static class Locator
    {
        private static IResolver _instance;

        public static void Init(IResolver resolver)
        {
            _instance = resolver;
        }
        
        public static void Shutdown()
        {
            _instance.Dispose();
        }

        public static T Resolve<T>()
        {
            return _instance.Resolve<T>();
        }

        public static T Resolve<T>(params Argument[] args)
        {
            return _instance.Resolve<T>(args);
        }

        public static T TryResolve<T>()
        {
            return _instance.TryResolve<T>();
        }

        public static T TryResolve<T>(string name)
        {
            return _instance.TryResolve<T>(name);
        }

        public static T TryResolve<T>(params Argument[] args)
        {
            return _instance.TryResolve<T>(args);
        }
    }
}
