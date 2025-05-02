using MicroRepository.Caching;
using MicroRepository.Core.DynamicParameters;
using MicroRepository.Core.Sql;
using MicroRepository.Repository.Interfaces;
using MicroRepository.Schema;
using MicroRepository.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MicroRepository.Repository.Repository
{
    public class ReadOnlyRepository<TEntity> : EnumerableRepository<TEntity>, IReadOnlyRepository<TEntity> where TEntity : class
    {
        protected readonly TableDescriptor TableDefinition;
        protected readonly ColumnDescriptor[] KeyColumns;

        public ReadOnlyRepository(IDbConnection connection) : base(connection)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));
            TableDefinition = TableDefinitionCache.GetTableDefinition(typeof(TEntity));
            KeyColumns = TableDefinition.Members.Values
                .Where(member => member.IsPrimaryKey)
                .ToArray();
        }

        public virtual TEntity? Find(params object[] orderedKeyValues)
        {
            if (KeyColumns.Length == 0)
            {
                throw new InvalidOperationException($"Table {TableDefinition.Name} does not have primary keys.");
            }
            var builder = new SqlBuilder(TableDefinition.SelectTemplate);
            BuildPrimaryKeyCondition(builder, orderedKeyValues);
            builder.Take(1);
            return Connection.QueryFirstOrDefault<TEntity>(builder.RawSql, builder.Parameters);
        }

        public IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object? parameter = null)
        {
            ArgumentNullException.ThrowIfNull(sqlQuery);
            return Connection.Query<TEntity>(sqlQuery, new DynamicParameter(parameter));
        }

        protected void BuildPrimaryKeyCondition(SqlBuilder builder, params object[] orderedKeyValues)
        {
            if (orderedKeyValues.Length != KeyColumns.Length)
            {
                throw new ArgumentException("The number of provided key values does not match the number of primary keys.");


            }
            for (var i = 0; i < KeyColumns.Length; i++)
            {
                var key = KeyColumns[i];
                var value = orderedKeyValues[i];
                if (value != null)
                {
                    builder.Where(key.UpdateString);
                    builder.AddParameter(key.Name, value);
                }
                else
                {
                    builder.Where($"{key.DbName} IS NULL");
                }
            }
        }

        protected TEntity FindEntityByPrimaryKey(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var builder = new SqlBuilder(TableDefinition.SelectTemplate);
            BindKeyColumn(entity, builder);
            builder.Take(1);

            return Connection.QueryFirstOrDefault<TEntity>(builder.RawSql, builder.Parameters)
                   ?? throw new InvalidOperationException("Entity not found after insert/update.");
        }

        protected void BindKeyColumn(TEntity entity, SqlBuilder builder)
        {
            foreach (var key in KeyColumns)
            {
                var value = key.Get(entity);
                if (value != null)
                {
                    builder.Where(key.UpdateString);
                    builder.AddParameter(key.Name, value);
                }
                else
                {
                    builder.Where($"{key.DbName} IS NULL");
                }
            }
        }
    }
}
