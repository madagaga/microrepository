using MicroRepository.Core.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MicroRepository.Core.Caching
{
    internal static class ReflectionCache
    {
        // Cache for storing property accessors
        static ConcurrentDictionary<Type, Dictionary<string, CompiledPropertyAccessor<object>>> _propertyCache = new ConcurrentDictionary<Type, Dictionary<string, CompiledPropertyAccessor<object>>>();

        // Cache for storing parameterless constructors
        static ConcurrentDictionary<Type, Delegate> _parameterLessConstructorCache = new ConcurrentDictionary<Type, Delegate>();

        // Initializes the cache for the specified type
        ///<summary>
        /// Initializes the cache for the specified type
        ///</summary>
        internal static void initializeCache(Type type)
        {
            if (!_propertyCache.ContainsKey(type))
                _propertyCache.TryAdd(
                    type,
                    type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(c => c.GetMethod != null && c.SetMethod != null)
                        .Select(p => new CompiledPropertyAccessor<object>(p))
                        .ToDictionary(c => c.Property.Name, c => c));

            if (!_parameterLessConstructorCache.ContainsKey(type))
            {
                _parameterLessConstructorCache.TryAdd(
                    type,
                    Expression.Lambda(Expression.New(type)).Compile());
            }
        }

        // Gets the properties of the specified type from the cache
        ///<summary>
        /// Gets the properties of the specified type from the cache
        ///</summary>
        public static Dictionary<string, CompiledPropertyAccessor<object>> GetProperties(Type type)
        {
            initializeCache(type);
            return _propertyCache[type];
        }

        // Gets the parameterless constructor of the specified type from the cache
        ///<summary>
        /// Gets the parameterless constructor of the specified type from the cache
        ///</summary>
        public static Delegate GetConstructor(Type type)
        {
            initializeCache(type);
            return _parameterLessConstructorCache[type];
        }

        // Creates an instance of the specified type using the parameterless constructor from the cache
        ///<summary>
        /// Creates an instance of the specified type using the parameterless constructor from the cache
        ///</summary>
        public static object CreateInstance(Type type)
        {
            initializeCache(type);
            return _parameterLessConstructorCache[type].DynamicInvoke();
        }
    }
}
