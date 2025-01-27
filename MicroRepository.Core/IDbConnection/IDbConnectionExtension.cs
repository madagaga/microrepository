using MicroRepository.Core.Caching;
using MicroRepository.Core.DynamicParameters;
using MicroRepository.Core.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MicroRepository.Core.Sql
{
    public static class IDbConnectionExtension
    {
        private static readonly ConcurrentDictionary<string, Dictionary<int, CompiledPropertyAccessor<object>>> _sqlPropertyMappingCache = new();

        // Executes a query without results
        public static int Execute(this IDbConnection connection, string sql, DynamicParameter? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            ArgumentNullException.ThrowIfNull(sql, nameof(sql));

            try
            {
                connection.OpenIfNeeded();
                using var command = connection.CreateCommand();
                BindParameters(command, parameters);
                command.CommandText = sql;
                return command.ExecuteNonQuery();
            }
            finally
            {
                connection.CloseIfNeeded();
            }
        }

        // Executes a query and returns a collection of T objects
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, DynamicParameter? parameters = null, bool buffered = true)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(sql);

            try
            {
                connection.OpenIfNeeded();
                using var command = connection.CreateCommand();
                BindParameters(command, parameters);
                command.CommandText = sql;

                var result = buffered
                    ? ExecuteReader<T>(command).ToList()
                    : ExecuteReader<T>(command);

                return result;
            }
            finally
            {
                connection.CloseIfNeeded();
            }
        }

        // Executes a query and returns the first row mapped to T
        public static T QueryFirst<T>(this IDbConnection connection, string sql, DynamicParameter? parameters = null)
        {
            return Query<T>(connection, sql, parameters, true).First();
        }

        // Executes a query and returns the first row or default mapped to T
        public static T? QueryFirstOrDefault<T>(this IDbConnection connection, string sql, DynamicParameter? parameters = null)
        {
            return Query<T>(connection, sql, parameters, true).FirstOrDefault();
        }

        // Executes a scalar query and returns a single value casted to T
        public static T ExecuteScalar<T>(this IDbConnection connection, string sql, DynamicParameter? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(sql);

            try
            {
                connection.OpenIfNeeded();
                using var command = connection.CreateCommand();
                BindParameters(command, parameters);
                command.CommandText = sql;

                var result = command.ExecuteScalar();
                return result is null ? default! : (T)Convert.ChangeType(result, typeof(T));
            }
            finally
            {
                connection.CloseIfNeeded();
            }
        }

        // Internal: Execute a reader and yield T objects
        private static IEnumerable<T> ExecuteReader<T>(IDbCommand command, CommandBehavior behavior = CommandBehavior.CloseConnection)
        {
            ArgumentNullException.ThrowIfNull(command);

            var targetType = typeof(T);
            command.Connection.OpenIfNeeded();

            using var reader = command.ExecuteReader(behavior);
            if (!targetType.IsPrimitiveType())
            {
                var cachedMapping = GetCachedMapping(command.CommandText, reader, targetType);
                var rowData = new object[reader.FieldCount];

                while (reader.Read())
                {
                    reader.GetValues(rowData);
                    var instance = (T)ReflectionCache.CreateInstance(targetType);

                    foreach (var kvp in cachedMapping)
                    {
                        var valueToSet = rowData[kvp.Key] is DBNull
                            ? null
                            : Convert.ChangeType(rowData[kvp.Key], kvp.Value.Type);
                        kvp.Value.Set(instance, valueToSet);
                    }

                    yield return instance;
                }
            }
            else
            {
                while (reader.Read())
                {
                    yield return (T)Convert.ChangeType(reader[0], Nullable.GetUnderlyingType(targetType) ?? targetType);
                }
            }
        }

        // Builds or retrieves mappings between reader columns and properties
        private static Dictionary<int, CompiledPropertyAccessor<object>> GetCachedMapping(string sql, IDataReader reader, Type targetType)
        {
            if (!_sqlPropertyMappingCache.TryGetValue(sql, out var mapping))
            {
                var properties = ReflectionCache.GetProperties(targetType).ToDictionary(p => p.Key.ToLower(), p => p.Value);
                var columnMapping = new Dictionary<int, CompiledPropertyAccessor<object>>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i).ToLower();
                    if (properties.TryGetValue(columnName, out var accessor))
                    {
                        columnMapping[i] = accessor;
                    }
                }

                _sqlPropertyMappingCache[sql] = columnMapping;
                return columnMapping;
            }

            return mapping;
        }

        // Parameter binding
        private static void BindParameters(IDbCommand command, DynamicParameter? parameters)
        {
            if (parameters is null) return;

            foreach (var kvp in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = kvp.Key;
                parameter.Value = kvp.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        // Opens connection if not already open
        private static void OpenIfNeeded(this IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        // Closes connection if not already closed
        private static void CloseIfNeeded(this IDbConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }
    }
}