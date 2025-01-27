using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MicroRepository.Core.Schema
{
    /// <summary>
    /// Provides a compiled property accessor for a generic type,
    /// enabling fast and efficient property access.
    /// </summary>
    /// <typeparam name="T">The type of the object containing the property.</typeparam>
    public class CompiledPropertyAccessor<T>
    {
        /// <summary>
        /// Compiled function to set the property value.
        /// </summary>
        private readonly Action<T, object> _setter;

        /// <summary>
        /// Compiled function to get the property value.
        /// </summary>
        private readonly Func<T, object> _getter;

        /// <summary>
        /// Gets the reflected PropertyInfo for the accessed property.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the CompiledPropertyAccessor.
        /// </summary>
        /// <param name="property">The property to create an accessor for.</param>
        /// <exception cref="ArgumentNullException">Thrown when property is null.</exception>
        public CompiledPropertyAccessor(PropertyInfo property)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Type = property.PropertyType;

            _setter = CreateSetter(property);
            _getter = CreateGetter(property);
        }

        /// <summary>
        /// Retrieves the value of the property for a given entity.
        /// </summary>
        /// <param name="entity">The entity to get the property value from.</param>
        /// <returns>The property value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
        public object Get(T entity) =>
            entity is null
                ? throw new ArgumentNullException(nameof(entity))
                : _getter(entity);

        /// <summary>
        /// Sets the value of the property for a given entity.
        /// </summary>
        /// <param name="entity">The entity to set the property value on.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when setting the value fails.</exception>
        public void Set(T entity, object? value)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                _setter(entity, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Unable to set value '{value}' on property {Property.Name}",
                    ex);
            }
        }

        /// <summary>
        /// Copies the property value from one entity to another.
        /// </summary>
        /// <param name="from">The source entity.</param>
        /// <param name="to">The destination entity.</param>
        /// <exception cref="ArgumentNullException">Thrown when either from or to is null.</exception>
        public void Copy(T from, T to)
        {
            ArgumentNullException.ThrowIfNull(from);
            ArgumentNullException.ThrowIfNull(to);

            Set(to, Get(from));
        }

        /// <summary>
        /// Creates a compiled setter for the specified property.
        /// </summary>
        /// <param name="property">The property to create a setter for.</param>
        /// <returns>A compiled setter function.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property cannot be set.</exception>
        private static Action<T, object> CreateSetter(PropertyInfo property)
        {
            // Check if the property can be written
            if (!property.CanWrite)
                return (_, __) => throw new InvalidOperationException(
                    $"Property {property.Name} does not have a setter");

            var entityParam = Expression.Parameter(typeof(T), "entity");
            var valueParam = Expression.Parameter(typeof(object), "value");

            var convertedValue = Expression.Convert(valueParam, property.PropertyType);
            var propertyAccess = Expression.Property(
                Expression.TypeAs(entityParam, property.DeclaringType),
                property
            );

            var assign = Expression.Assign(propertyAccess, convertedValue);
            return Expression.Lambda<Action<T, object>>(
                assign,
                entityParam,
                valueParam
            ).Compile();
        }

        /// <summary>
        /// Creates a compiled getter for the specified property.
        /// </summary>
        /// <param name="property">The property to create a getter for.</param>
        /// <returns>A compiled getter function.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property cannot be read.</exception>
        private static Func<T, object> CreateGetter(PropertyInfo property)
        {
            // Check if the property can be read
            if (!property.CanRead)
                return _ => throw new InvalidOperationException(
                    $"Property {property.Name} does not have a getter");

            var entityParam = Expression.Parameter(typeof(T), "entity");

            var propertyAccess = Expression.Property(
                Expression.TypeAs(entityParam, property.DeclaringType),
                property
            );

            var convertToObject = Expression.Convert(propertyAccess, typeof(object));

            return Expression.Lambda<Func<T, object>>(
                convertToObject,
                entityParam
            ).Compile();
        }

        /// <summary>
        /// Gets the default value for the property type.
        /// </summary>
        /// <returns>The default value for value types, null for reference types.</returns>
        public object? GetDefaultValue() =>
            Type.IsValueType ? default(T) : null;
    }
}