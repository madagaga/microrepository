using MicroRepository.Core.Caching;
using MicroRepository.Core.DynamicParameters;
using MicroRepository.Core.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

using System.Linq;
using System.Threading.Tasks;

namespace MicroRepository.Core.Sql
{
    public static class IDbConnectionExtension
    {
        static readonly ConcurrentDictionary<string, Dictionary<int, CompiledPropertyAccessor<object>>> _sqlPropertyMappingCache = new ConcurrentDictionary<string, Dictionary<int, CompiledPropertyAccessor<object>>>();

       
        public static int Execute(this IDbConnection connection, string sql, params object[] parameters)
        {
            return Execute(connection, sql, buildParameters(parameters));
        }

        public static int Execute(this IDbConnection connection, string sql, DynamicParameter parameters = null)
        {
            lock (connection)
            {
                try
                {                    
                    openConnection(connection);
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        buildParameters(command, parameters);
                        command.CommandText = sql;
                        return command.ExecuteNonQuery();                        
                    }                    
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    closeConnection(connection);
                }
            }
        }

        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, params object[] parameters)
        {
            return Query<T>(connection, sql, buildParameters(parameters));
        }
        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, DynamicParameter parameters = null, bool buffered = true)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("sql");
            lock (connection)
            {
                try
                {
                    
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        buildParameters(command, parameters);
                        command.CommandText = sql;
                        if (buffered)
                            return ExecuteReader<T>(command).ToList();
                        else
                            return ExecuteReader<T>(command);


                    }
                    
                }
                catch (Exception e)
                {
                    closeConnection(connection);
                    throw new Exception("IEnumerable<T> Query<T>", e);                    
                }
                

            }
            
        }

        private static IEnumerable<T> ExecuteReader<T>(IDbCommand command, CommandBehavior commandBehavior = CommandBehavior.CloseConnection)
        {
            Type targetType = typeof(T);
            openConnection(command.Connection);
            using (IDataReader reader = command.ExecuteReader(commandBehavior))
            {
                if (!PrimitiveTypes.IsPrimitive(targetType))
                {
                    T instance = default(T);

                    Dictionary<int, CompiledPropertyAccessor<object>> types = GetCachedMapping(command, reader, targetType);
                    object[] rowData = new object[reader.FieldCount];
                    while (reader.Read())
                    {
                        reader.GetValues(rowData);
                        instance = (T)ReflectionCache.CreateInstance(targetType);

                        foreach (var kvp in types)
                        {
                            if (PrimitiveTypes.IsPrimitive(kvp.Value.Type) && rowData[kvp.Key] != DBNull.Value)
                                kvp.Value.Set(instance, Convert.ChangeType(rowData[kvp.Key], kvp.Value.Type));
                            else
                                kvp.Value.Set(instance, rowData[kvp.Key] is DBNull ? null : rowData[kvp.Key]);
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
            closeConnection(command.Connection);
        }

        public static T QueryFirst<T>(this IDbConnection connection, string sql, params object[] parameters)
        {
            return QueryFirst<T>(connection, sql, buildParameters(parameters));
        }

        public static T QueryFirst<T>(this IDbConnection connection, string sql, DynamicParameter parameters = null)
        {
            T instance = default(T);
            lock (connection)
            {
                try
                {

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        buildParameters(command, parameters);
                        command.CommandText = sql;
                        instance = ExecuteReader<T>(command, CommandBehavior.SingleResult).First();
                    }

                }
                catch (Exception e)
                {
                    closeConnection(connection);
                    throw e;
                }


            }

            return instance;
        }

        public static T QueryFirstOrDefault<T>(this IDbConnection connection, string sql, params object[] parameters)
        {
            return QueryFirstOrDefault<T>(connection, sql, buildParameters(parameters));
        }
        public static T QueryFirstOrDefault<T>(this IDbConnection connection, string sql, DynamicParameter parameters = null)
        {
            T instance = default(T);
            try
            {
                instance = QueryFirst<T>(connection, sql, parameters);
            }
            catch { }
            return instance;
        }

        public static T ExecuteScalar<T>(this IDbConnection connection, string sql, params object[] parameters)
        {
            return ExecuteScalar<T>(connection, sql, buildParameters(parameters));
        }
        public static T ExecuteScalar<T>(this IDbConnection connection, string sql, DynamicParameter parameters = null)
        {
            lock (connection)
            {
                try
                {
                    openConnection(connection);
                    using (IDbCommand command = connection.CreateCommand())
                    {
                        buildParameters(command, parameters);
                        command.CommandText = sql;
                        object result = command.ExecuteScalar();
                        if (result == null)
                            return default(T);
                        else 
                            return (T) Convert.ChangeType(result, typeof(T));
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    closeConnection(connection);
                }
            }
        }



        private static IEnumerable<DbRow> ExecuteReader(IDbCommand command, CommandBehavior commandBehavior = CommandBehavior.CloseConnection)
        {
            List<DbRow> result = new List<DbRow>();
            openConnection(command.Connection);
            using (IDataReader reader = command.ExecuteReader(commandBehavior))
            {
                List<string> columns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    columns.Add(reader.GetName(i));

                object[] rowData = new object[reader.FieldCount];
                while (reader.Read())
                {
                    reader.GetValues(rowData);
                    result.Add(new DbRow(columns, rowData));
                }
            }
            closeConnection(command.Connection);
            return result;
        }

        public static IEnumerable<DbRow> Query(this IDbConnection connection, string sql, params object[] parameters)
        {
            return Query(connection, sql, buildParameters(parameters));
        }

        public static IEnumerable<DbRow> Query(this IDbConnection connection, string sql, DynamicParameter parameters = null)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentNullException("sql");
            lock (connection)
            {
                try
                {

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        buildParameters(command, parameters);
                        command.CommandText = sql;                        
                        return ExecuteReader(command);

                    }

                }
                catch (Exception e)
                {
                    closeConnection(connection);
                    throw new Exception("DbTable Query", e);
                }


            }
        }


        #region internal
        static void openConnection(IDbConnection _connection)
        {
            if (_connection != null && _connection.State != ConnectionState.Open)
                _connection.Open();
        }

        static void closeConnection(IDbConnection _connection)
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        static void buildParameters(IDbCommand command, DynamicParameter parameters = null)
        {
            if (parameters == null)
                return;

            IDbDataParameter parameter;            
            foreach(var kvp in parameters)
            {                
                parameter = command.CreateParameter();
                parameter.ParameterName = kvp.Key;
                parameter.Value = kvp.Value;
                command.Parameters.Add(parameter);
            }            
        }

        static DynamicParameter buildParameters(object[] parameters)
        {
            if (parameters != null && parameters.Length > 0)
            {

                DynamicParameter result = new DynamicParameter();
                for (int i = 0; i < parameters.Length; i++)
                    result.Add($"p{i}", parameters[i]);
                return result;
            }
            return null;
        }

        static Dictionary<int, CompiledPropertyAccessor<object>>  GetCachedMapping(IDbCommand command, IDataReader reader, Type targetType)
        {
            // compute hash 
            string hash = Hash(command.CommandText);
            lock (_sqlPropertyMappingCache)
            {                
                if (!_sqlPropertyMappingCache.ContainsKey(hash))
                {
                    var properties = ReflectionCache.GetProperties(targetType).ToDictionary(c => c.Key.ToLower(), c => c.Value);
                    Dictionary<int, CompiledPropertyAccessor<object>> types = new Dictionary<int, CompiledPropertyAccessor<object>>();
                    string columnName;
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columnName = reader.GetName(i).ToLower();
                        if (properties.ContainsKey(columnName))
                            types.Add(i, properties[columnName]);
                    }
                    _sqlPropertyMappingCache.TryAdd(hash, types);
                }
            }
            return _sqlPropertyMappingCache[hash];

        }

        static string Hash(string content)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(content);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string                    
                return string.Join("", hashBytes.Select(c => c.ToString("X2")));
            }
        }


        #endregion
    }
}
