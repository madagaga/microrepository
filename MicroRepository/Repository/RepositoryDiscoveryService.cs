using MicroRepository.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository
{
    internal static class RepositoryDiscoveryService
    {
        static ConcurrentDictionary<Type, PropertyInfo[]> _reflectionCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        static ConcurrentDictionary<Type, Delegate> _constructorCache = new ConcurrentDictionary<Type, Delegate>();
        static Type _interface = typeof(IRepository);


        // sql templates repository
        public static void Initialize(Repositories context)
        {   

            // initialize all dbsets

            Type repositoryManagerType = context.GetType();

            PropertyInfo[] properties = null;
            if (!_reflectionCache.ContainsKey(repositoryManagerType))
            {
                properties = repositoryManagerType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.PropertyType.GetInterfaces().Contains(_interface)).ToArray();                
                _reflectionCache.TryAdd(repositoryManagerType, properties);
            }
            else
                properties = _reflectionCache[repositoryManagerType];

            foreach (PropertyInfo repositoryProperty in properties)
            {
                object repository = CreateInstance(repositoryProperty.PropertyType, context.Connection);
                context._repositoryCache.Add(repositoryProperty.PropertyType.GetGenericArguments().First(), repository);
                repositoryProperty.SetValue(context, repository, null);
            }
        }

        static object CreateInstance(Type type, params object[] args)
        {

            if (_constructorCache.ContainsKey(type))
                return _constructorCache[type].DynamicInvoke(args);

            Type[] argumentTypes = args.Where(c => c != null).Select(c => c.GetType()).ToArray();

            Delegate constructor = getConstructor(type, argumentTypes);

            _constructorCache.TryAdd(type, constructor);
            return constructor.DynamicInvoke(args);
        }

        static Delegate getConstructor(Type type, Type[] parametersType)
        {
            // Get the Constructor which matches the given argument Types:
            ConstructorInfo constructor = type.GetConstructor( parametersType);

            if (constructor == null)
                throw new ArgumentException("No constructor exist with this paramater");

            int i = 0;
            var lambdaParameter = parametersType.Select(p => Expression.Parameter(p, string.Format("param{0}", ++i))).ToArray();
            var constructorCallExpression = Expression.New(constructor, lambdaParameter.Take(parametersType.Length));
            return Expression
               .Lambda(constructorCallExpression, lambdaParameter)
               .Compile();

        }
    }
}
