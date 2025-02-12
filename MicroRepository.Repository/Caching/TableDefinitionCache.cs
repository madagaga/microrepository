using MicroRepository.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MicroRepository.Caching
{
    public static class TableDefinitionCache
    {
        internal static ConcurrentDictionary<Type, TableDescriptor> _tableDefinitions = new ConcurrentDictionary<Type, TableDescriptor>();

        internal static TableDescriptor GetTableDefinition(Type targetType)
        {
            if (!_tableDefinitions.ContainsKey(targetType))
            {
                TableDescriptor definition = new TableDescriptor(targetType);
                _tableDefinitions.TryAdd(targetType, definition);
            }
            return _tableDefinitions[targetType];
        }

        internal static Dictionary<string, ColumnDescriptor> GetPropertiesDictionary(Type t)
        {

            return GetTableDefinition(t).Members;
        }
    }
}
