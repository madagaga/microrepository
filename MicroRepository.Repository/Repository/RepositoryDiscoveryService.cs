using MicroRepository.Enums;
using MicroRepository.Repository.Interfaces;
using MicroRepository.Templates;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
            var lambdaParameter = parametersType.Select(p => Expression.Parameter(p, $"param{++i}")).ToArray();
            var constructorCallExpression = Expression.New(constructor, lambdaParameter.Take(parametersType.Length));
            return Expression
               .Lambda(constructorCallExpression, lambdaParameter)
               .Compile();

        }


        #region configuration purpose
        public static bool Buffered { get; set; } = true;
        public static bool UpdateChangeOnly { get; set; } = true;
        static SqlTemplate _template;
        internal static SqlTemplate Template
        {
            get
            {
                if (_dataBaseType == DatabaseType.Auto)
                    throw new ArgumentException("Databasetype can not be auto when accessing to template.");
                return _template;
            }
        }

        static DatabaseType _dataBaseType = DatabaseType.Auto;
        internal static DatabaseType DataBaseType
        {
            get { return _dataBaseType; }
            set
            {
                if (value == DatabaseType.Auto)
                    throw new ArgumentException("Database type can not be set to Auto");

                _dataBaseType = value;
                if (_template == null)
                    _template = SqlTemplate.Load(_dataBaseType.ToString().ToLower());
            }
        }
        #endregion        

        internal static object DiagnosticString()
        {
            return string.Format("** RepositoryConfiguration **\r\nUpdateChangeOnly : {0}\r\nDatabase Type : {1}", UpdateChangeOnly, DataBaseType);
        }
    }
}
