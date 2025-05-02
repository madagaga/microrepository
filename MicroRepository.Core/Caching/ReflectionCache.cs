using MicroRepository.Core.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MicroRepository.Core.Caching
{
    /// <summary>
    /// Provides caching mechanisms for reflection operations to improve performance.
    /// </summary>
    internal static class ReflectionCache
    {
        /// <summary>
        /// Thread-safe cache for property accessors.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, CompiledPropertyAccessor<object>>>
            PropertyCache = new();

        /// <summary>
        /// Thread-safe cache for parameterless constructors.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Func<object>>
            ConstructorCache = new();

        /// <summary>
        /// Initializes cache entries for a specified type.
        /// </summary>
        /// <param name="type">The type to initialize cache for.</param>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        private static void InitializeCache(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            PropertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.GetMethod != null && p.SetMethod != null)
                    .Select(p => new CompiledPropertyAccessor<object>(p))
                    .ToDictionary(c => c.Property.Name, c => c));

            ConstructorCache.GetOrAdd(type, t =>
                Expression.Lambda<Func<object>>(Expression.New(t)).Compile());
        }

        /// <summary>
        /// Gets cached property accessors for a specified type.
        /// </summary>
        /// <param name="type">The type to get properties for.</param>
        /// <returns>Dictionary of property accessors.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static Dictionary<string, CompiledPropertyAccessor<object>> GetProperties(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return PropertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.GetMethod != null && p.SetMethod != null)
                    .Select(p => new CompiledPropertyAccessor<object>(p))
                    .ToDictionary(c => c.Property.Name, c => c));
        }

        /// <summary>
        /// Gets cached constructor for a specified type.
        /// </summary>
        /// <param name="type">The type to get constructor for.</param>
        /// <returns>Compiled constructor delegate.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        /// <exception cref="MissingMethodException">Thrown when parameterless constructor is not found.</exception>
        public static Func<object> GetConstructor(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return ConstructorCache.GetOrAdd(type, t =>
            {
                var ctor = t.GetConstructor(Type.EmptyTypes)
                    ?? throw new MissingMethodException($"No parameterless constructor found for type {t.Name}");

                return Expression.Lambda<Func<object>>(Expression.New(t)).Compile();
            });
        }

        /// <summary>
        /// Creates an instance of specified type using cached constructor.
        /// </summary>
        /// <param name="type">The type to create instance of.</param>
        /// <returns>New instance of specified type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static object CreateInstance(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return GetConstructor(type)();
        }

        /// <summary>
        /// Creates a strongly typed instance of specified type.
        /// </summary>
        /// <typeparam name="T">The type to create instance of.</typeparam>
        /// <returns>New instance of specified type.</returns>
        public static T CreateInstance<T>() where T : new()
        {
            return (T)CreateInstance(typeof(T));
        }

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        public static void ClearCache()
        {
            PropertyCache.Clear();
            ConstructorCache.Clear();
        }
    }
}