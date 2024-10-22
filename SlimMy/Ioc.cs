using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy
{
    public class Ioc
    {
        private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

        public static void Register<T>(T instance)
        {
            _instances[typeof(T)] = instance;
        }

        public static T Resolve<T>()
        {
            return (T)_instances[typeof(T)];
        }
    }
}
