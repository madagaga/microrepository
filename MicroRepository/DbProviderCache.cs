using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MicroRepository
{
    public static class DbProviderCache
    {
        public static void RegisterProvider(Type factoryType)
        {
            if (!_factoryCache.ContainsKey(factoryType.ToString()))
                _factoryCache.Add(factoryType.ToString(),(DbProviderFactory) Caching.ReflectionCache.CreateInstance(factoryType));
        }

        static Dictionary<string, DbProviderFactory> _factoryCache = new Dictionary<string, DbProviderFactory>();

        public static DbProviderFactory GetFactory(string providerName)
        {
            if (_factoryCache.ContainsKey(providerName))
                return _factoryCache[providerName];

            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(providerName);
            if (dbProviderFactory != null)
            {
                _factoryCache.Add(providerName, dbProviderFactory);
                return dbProviderFactory;
            }

            AssemblyName an = new AssemblyName(providerName);
            // dynamically load 
            var assembly = Assembly.Load(an);
            Type p = typeof(DbProviderFactory);
            Type providerType = assembly.GetTypes().FirstOrDefault(c => c == p);

            if (null != providerType)
            {

                System.Reflection.FieldInfo providerInstance = providerType.GetField("Instance", System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (null != providerInstance)
                {
                    Debug.Assert(providerInstance.IsPublic, "field not public");
                    Debug.Assert(providerInstance.IsStatic, "field not static");

                    if (providerInstance.FieldType.GetTypeInfo().IsSubclassOf(typeof(DbProviderFactory)))
                    {

                        object factory = providerInstance.GetValue(null);
                        if (null != factory)
                        {
                            dbProviderFactory = (DbProviderFactory)factory;
                            _factoryCache.Add(providerName, dbProviderFactory);
                        }
                        // else throw ConfigProviderInvalid
                    }
                    // else throw ConfigProviderInvalid
                }
            }

            return dbProviderFactory;
        }
    }
}
