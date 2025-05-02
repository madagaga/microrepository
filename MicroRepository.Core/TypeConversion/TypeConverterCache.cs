using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Core.TypeConversion
{
    public static class TypeConverterCache
    {
        private static readonly ConcurrentDictionary<(Type source, Type target), Func<object, object>> Converters
            = new ConcurrentDictionary<(Type source, Type target), Func<object, object>>();

        private static readonly ConcurrentDictionary<(Type source, Type target), Func<object, object>> CustomConverters
            = new ConcurrentDictionary<(Type source, Type target), Func<object, object>>();

        private static ConcurrentDictionary<Type, Type> registeredTypes = new ConcurrentDictionary<Type, Type>();
        
        public static void RegisterConverter<TSource, TTarget>(Func<TSource, TTarget> converter)
        {
            CustomConverters[(typeof(TSource), typeof(TTarget))] = (obj) => converter((TSource)obj)!;
            registeredTypes[typeof(TSource)] = typeof(TTarget);
        }

        

        public static object? ConvertFrom(object? value)
        {
            if (value == null) return null;
            Type sourceType = value.GetType();
            

            if(registeredTypes.TryGetValue(sourceType, out Type targetType))
            {
                if (CustomConverters.TryGetValue((sourceType, targetType), out var customConverter))
                {
                    return customConverter(value);
                }
                return value;
            }
            return value;
        }

        public static object? ConvertTo(object? value, Type targetType)
        {
            if (value == null) return null;

            var sourceType = value.GetType();

            // Vérifie d'abord s'il existe un convertisseur personnalisé
            if (CustomConverters.TryGetValue((sourceType, targetType), out var customConverter))
            {
                return customConverter(value);
            }

            // Sinon utilise le convertisseur par défaut
            var converter = Converters.GetOrAdd((sourceType, targetType), types =>
            {
                var parameter = Expression.Parameter(typeof(object));
                var convertExpr = Expression.Convert(
                    Expression.Convert(parameter, types.source),
                    types.target
                );
                var finalConvert = Expression.Convert(convertExpr, typeof(object));
                return Expression.Lambda<Func<object, object>>(finalConvert, parameter).Compile();
            });

            return converter(value);
        }
    }
}
