using MicroRepository.Caching;
using MicroRepository.DynamicParameters;
using System;
using System.Collections.Generic;
using System.Data;

using System.Linq;
using System.Threading.Tasks;

namespace MicroRepository.Sql
{
    public static class IDbConnectionExtension
    {
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

        public static IEnumerable<T> Query<T>(this IDbConnection connection, string sql, DynamicParameter parameters = null)
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
                        if (DbFactory.Buffered)
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
                    var properties = ReflectionCache.GetProperties(targetType);
                    T instance = default(T);
                    List<string> columns = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        columns.Add(reader.GetName(i));

                    object[] rowData = new object[reader.FieldCount];
                    while (reader.Read())
                    {
                        reader.GetValues(rowData);
                        instance = (T)ReflectionCache.CreateInstance(targetType);                        
                        for (int i = 0; i < columns.Count; i++)
                        {
                            if (properties.ContainsKey(columns[i]))
                                properties[columns[i]].Set(instance, rowData[i] is DBNull ? null : rowData[i]);
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
                
        #endregion
    }
}
