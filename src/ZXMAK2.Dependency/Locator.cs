

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

        public static T TryResolve<T>()
        {
            return _instance.TryResolve<T>();
        }
    }
}
