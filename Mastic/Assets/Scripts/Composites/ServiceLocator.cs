using System;

namespace Mastic
{
    public static class ServiceLocator<T>
    {
        private static T instance;

        public static T Locate()
        {
            if (instance == null)
                throw new Exception("No service provided yet.");

            return instance;
        }

        public static bool HasService() => instance != null;
        public static void Provide(T service) => instance = service;
    }
}