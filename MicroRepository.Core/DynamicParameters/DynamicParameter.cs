using MicroRepository.Core.Caching;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MicroRepository.Core.DynamicParameters
{
    /// <summary>
    /// Represents a parameter dictionary with dynamic parameter support.
    /// Compatible with NativeAOT.
    /// </summary>
    public sealed class DynamicParameter : Dictionary<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the DynamicParameter class.
        /// </summary>
        public DynamicParameter() : base(StringComparer.OrdinalIgnoreCase) { }

        /// <summary>
        /// Initializes a new instance of the DynamicParameter class with the specified parameters.
        /// </summary>
        /// <param name="parameters">The initial parameters.</param>
        public DynamicParameter(object? parameters) : this()
        {
            AddDynamicParams(parameters);
        }

        /// <summary>
        /// Adds parameters from an object using compiled property accessors.
        /// </summary>
        /// <param name="parameters">The object containing parameters.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDynamicParams(object? parameters)
        {
            if (parameters == null) return;

            switch (parameters)
            {
                case DynamicParameter dynamicParam:
                    Merge(dynamicParam);
                    break;

                case IDictionary<string, object> dictionary:
                    Merge(dictionary);
                    break;

                case IEnumerable<KeyValuePair<string, object>> kvpEnumerable:
                    foreach (var kvp in kvpEnumerable)
                    {
                        this[kvp.Key] = kvp.Value;
                    }
                    break;

                default:
                    AddObjectProperties(parameters);
                    break;
            }
        }

        /// <summary>
        /// Adds properties from an object using compiled accessors.
        /// </summary>
        /// <param name="obj">The object to extract properties from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddObjectProperties(object obj)
        {
            var properties = ReflectionCache.GetProperties(obj.GetType());

            foreach (var property in properties)
            {
                var value = property.Value.Get(obj);
                if (value == null) continue;

                if (value is IDictionary<string, object> dict)
                {
                    Merge(dict);
                }
                else
                {
                    this[property.Key] = value;
                }
            }
        }

        /// <summary>
        /// Merges parameters from another dictionary.
        /// </summary>
        /// <param name="parameters">The dictionary to merge from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Merge(IDictionary<string, object> parameters)
        {
            foreach (var kvp in parameters)
            {
                this[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Tries to get a parameter value.
        /// </summary>
        /// <typeparam name="T">The expected type of the parameter.</typeparam>
        /// <param name="key">The parameter key.</param>
        /// <param name="value">The output value.</param>
        /// <returns>True if the parameter exists and is of the correct type.</returns>
        public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
        {
            if (base.TryGetValue(key, out object? objValue) && objValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Gets a parameter value with type conversion.
        /// </summary>
        /// <typeparam name="T">The expected type of the parameter.</typeparam>
        /// <param name="key">The parameter key.</param>
        /// <param name="defaultValue">The default value if the parameter doesn't exist.</param>
        /// <returns>The parameter value or default value.</returns>
        public T GetValue<T>(string key, T defaultValue = default!)
        {
            if (TryGetValue<T>(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}