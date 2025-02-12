using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MicroRepository.Schema
{
    /// <summary>
    /// Tracks changes between two instances of the same type and allows partial or full updates.
    /// </summary>
    public class Delta<T> where T : class
    {
        private readonly Dictionary<string, ColumnDescriptor> _properties;
        private readonly HashSet<string> _ignoredProperties;
        private readonly HashSet<string> _changedProperties;
        private readonly T _entity;

        /// <summary>
        /// Initializes a new instance of the <see cref="Delta{T}"/> class.
        /// </summary>
        /// <param name="entity">The entity representing the "changed" state.</param>
        public Delta(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _entity = entity;
            _properties = Caching.TableDefinitionCache.GetPropertiesDictionary(typeof(T));
            _ignoredProperties = new HashSet<string>();
            _changedProperties = new HashSet<string>();
        }

        /// <summary>
        /// Excludes a property from being tracked.
        /// </summary>
        /// <param name="propertyName">The name of the property to exclude.</param>
        /// <returns>The current <see cref="Delta{T}"/> instance for chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property does not exist.</exception>
        public Delta<T> Exclude(string propertyName)
        {
            if (!_properties.ContainsKey(propertyName))
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not a member of '{typeof(T).Name}'.");
            }

            _ignoredProperties.Add(propertyName);
            return this;
        }

        /// <summary>
        /// Performs a partial update on the original entity using only the tracked changes.
        /// </summary>
        /// <param name="original">The original entity to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when the original entity is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the original entity type does not match <typeparamref name="T"/>.</exception>
        public void Patch(T original)
        {
            ArgumentNullException.ThrowIfNull(original);

            if (!_properties.ContainsKey(original.GetType().Name))
            {
                throw new ArgumentException($"Entity type mismatch: expected '{typeof(T).Name}'.", nameof(original));
            }

            Compare(original);

            foreach (var propertyAccessor in GetChangedPropertiesAccessors())
            {
                propertyAccessor.Copy(_entity, original);
            }
        }

        /// <summary>
        /// Performs a full update, copying all properties (both changed and unchanged).
        /// </summary>
        /// <param name="original">The original entity to update.</param>
        public void Put(T original)
        {
            Patch(original);

            foreach (var propertyAccessor in GetUnchangedPropertiesAccessors())
            {
                propertyAccessor.Copy(_entity, original);
            }
        }

        /// <summary>
        /// Compares the tracked entity with the original one and determines the changed properties.
        /// </summary>
        /// <param name="original">The original entity to compare against.</param>
        /// <param name="excludeNull">Whether to ignore null values during comparison.</param>
        public void Compare(T original, bool excludeNull = true)
        {
            _changedProperties.Clear();

            foreach (var property in _properties)
            {
                // Skip ignored properties
                if (_ignoredProperties.Contains(property.Key)) continue;

                var changedValue = property.Value.Get(_entity);
                var originalValue = property.Value.Get(original);

                if (changedValue == null)
                {
                    if (!excludeNull && originalValue != null)
                    {
                        _changedProperties.Add(property.Key);
                    }
                }
                else if (!changedValue.Equals(originalValue))
                {
                    _changedProperties.Add(property.Key);
                }
            }
        }

        /// <summary>
        /// Gets the names of the properties that have been changed.
        /// </summary>
        public IEnumerable<string> GetChangedPropertyNames()
        {
            return _changedProperties.ToList();
        }

        /// <summary>
        /// Gets the accessors of the properties that have been changed.
        /// </summary>
        public IEnumerable<ColumnDescriptor> GetChangedPropertiesAccessors()
        {
            return _changedProperties.Select(key => _properties[key]);
        }

        /// <summary>
        /// Gets the names of the properties that have not been changed.
        /// </summary>
        public IEnumerable<string> GetUnchangedPropertyNames()
        {
            return _properties.Keys.Except(_changedProperties).ToList();
        }

        /// <summary>
        /// Gets the accessors of the properties that have not been changed.
        /// </summary>
        public IEnumerable<ColumnDescriptor> GetUnchangedPropertiesAccessors()
        {
            return _properties.Keys
                .Except(_changedProperties)
                .Select(key => _properties[key]);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="Delta{T}"/>.
    /// </summary>
    public static class DeltaExtension
    {
        /// <summary>
        /// Excludes a property using a lambda expression selector.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <typeparam name="TKey">The type of the property.</typeparam>
        /// <param name="delta">The Delta instance to modify.</param>
        /// <param name="propertySelector">An expression selecting the property.</param>
        /// <returns>The updated Delta instance.</returns>
        public static Delta<T> Exclude<T, TKey>(this Delta<T> delta, Expression<Func<T, TKey>> propertySelector) where T : class
        {
            if (propertySelector.Body is not MemberExpression member)
            {
                throw new ArgumentException("Selector must be a valid member expression.", nameof(propertySelector));
            }

            return delta.Exclude(member.Member.Name);
        }
    }
}