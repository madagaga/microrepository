using MicroRepository.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Caching
{
    public static class TableDefinitionCache
    {
        internal static ConcurrentDictionary<Type, TableDefinition> _tableDefinitions = new ConcurrentDictionary<Type, TableDefinition>();

        internal static TableDefinition GetTableDefinition(Type targetType)
        {
            if (!_tableDefinitions.ContainsKey(targetType))
            {
                TableDefinition definition = new TableDefinition(targetType);
                _tableDefinitions.TryAdd(targetType, definition);
            }
            return _tableDefinitions[targetType];
        }

        internal static Dictionary<string, DataBasePropertyAccessor> GetPropertiesDictionary(Type t)
        {
            
            return GetTableDefinition(t).Members;
        }
    }
}
