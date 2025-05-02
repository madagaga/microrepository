using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroRepository.Core
{

    /// <summary>
    /// Represents the list of primitive types.
    /// </summary>
    static class PrimitiveTypes
    {
        private static readonly Type[] _primitiveTypes;
        private static readonly ConcurrentDictionary<Type, bool> _cache = new();

        static PrimitiveTypes()
        {
            var types = new[]
            {
                    typeof(String),
                    typeof(Char),
                    typeof(Guid),

                    typeof(Boolean),
                    typeof(Byte),
                    typeof(Int16),
                    typeof(Int32),
                    typeof(Int64),
                    typeof(Single),
                    typeof(Double),
                    typeof(Decimal),

                    typeof(SByte),
                    typeof(UInt16),
                    typeof(UInt32),
                    typeof(UInt64),

                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                };

            var nullableTypes = new[]
            {
                typeof(char?),
                typeof(Guid?),
                typeof(bool?),
                typeof(byte?),
                typeof(short?),
                typeof(int?),
                typeof(long?),
                typeof(float?),
                typeof(double?),
                typeof(decimal?),
                typeof(sbyte?),
                typeof(ushort?),
                typeof(uint?),
                typeof(ulong?),
                typeof(DateTime?),
                typeof(DateTimeOffset?),
                typeof(TimeSpan?)
            };

            _primitiveTypes = types.Concat(nullableTypes).ToArray();
        }

        /// <summary>
        /// Checks if the given type is a primitive type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a primitive type, otherwise false.</returns>
        public static bool IsPrimitiveType(this Type type)
        {
            if (_cache.TryGetValue(type, out bool result))
                return result;

            if (_primitiveTypes.Any(x => x.IsAssignableFrom(type)))
            {
                _cache.TryAdd(type, true);
                return true;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            var isNullableEnum = underlyingType?.IsEnum ?? false;

            if (isNullableEnum)
                _cache.TryAdd(type, true);

            return isNullableEnum;
        }

    }
}
