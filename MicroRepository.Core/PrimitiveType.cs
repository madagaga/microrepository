using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MicroRepository.Core
{

    /// <summary>
    /// Represents the list of primitive types.
    /// </summary>
    static class PrimitiveTypes
    {
        public static readonly Type[] List;

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

            var nullTypes = from t in types
                            where t.GetTypeInfo().IsValueType
                            select typeof(Nullable<>).MakeGenericType(t);

            List = types.Concat(nullTypes).ToArray();
        }

        /// <summary>
        /// Checks if the given type is a primitive type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a primitive type, otherwise false.</returns>
        public static bool IsPrimitive(Type type)
        {
            if (List.Any(x => x.IsAssignableFrom(type)))
                return true;

            var nut = Nullable.GetUnderlyingType(type);
            return nut != null && nut.GetTypeInfo().IsEnum;
        }
    }
}
