using MicroRepository.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MicroRepository.Caching
{
    internal static class ReflectionCache
    {
        static ConcurrentDictionary<Type, Dictionary<string, CompiledPropertyAccessor<object>>> _propertyCache = new ConcurrentDictionary<Type, Dictionary<string, CompiledPropertyAccessor<object>>>();
        static ConcurrentDictionary<Type, Delegate> _parameterLessConstructorCache = new ConcurrentDictionary<Type, Delegate>();
        internal static void initializeCache(Type type)
        {
            if (!_propertyCache.ContainsKey(type))
                _propertyCache.TryAdd(
                    type,
                    type.GetProperties()
                        .Where(c=>c.GetMethod != null && c.SetMethod != null)
                        .Select(p => new CompiledPropertyAccessor<object>(p))
                        .ToDictionary(c => c.Property.Name, c => c));

            if (!_parameterLessConstructorCache.ContainsKey(type))
            {
                _parameterLessConstructorCache.TryAdd(
                    type,
                    Expression.Lambda(Expression.New(type)).Compile());
            }
        }

        public static Dictionary<string, CompiledPropertyAccessor<object>> GetProperties(Type type)
        {
            initializeCache(type);
            return _propertyCache[type];
        }

        public static Delegate GetConstructor(Type type)
        {
            initializeCache(type);
            return _parameterLessConstructorCache[type];
        }

        public static object CreateInstance(Type type)
        {
            initializeCache(type);
            return _parameterLessConstructorCache[type].DynamicInvoke();
        }


    }
}
