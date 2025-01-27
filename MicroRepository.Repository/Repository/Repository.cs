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

namespace MicroRepository.Repository
{
    public partial class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly TableDefinition _tableDefinition;
        private readonly DataBasePropertyAccessor[] _keyColumns;

        public Repository(IDbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));

            Connection = connection;
            _tableDefinition = TableDefinitionCache.GetTableDefinition(typeof(TEntity));
            _keyColumns = _tableDefinition.Members.Values
                .Where(member => member.IsPrimaryKey)
                .ToArray();
        }

        #region IRepository

        public IDbConnection Connection { get; }

        public EnumerableRepository<TEntity> Elements => new EnumerableRepository<TEntity>(Connection);

        public virtual TEntity Add(TEntity item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var builder = new SqlBuilder(_tableDefinition.InsertTemplate);
            builder.AddParameter(item);

            if (_tableDefinition.HasIdentity)
            {
                var id = Connection.ExecuteScalar<int>(builder.RawSql, builder.Parameters);
                if (id <= 0)
                {
                    throw new InvalidOperationException("Insert failed: no identity key returned.");
                }

                return Find(id);
            }
            else
            {
                var rowsInserted = Connection.Execute(builder.RawSql, builder.Parameters);
                if (rowsInserted > 0)
                {
                    if (_keyColumns.Any())
                    {
                        return FindEntityByPrimaryKey(item);
                    }

                    return item;
                }

                throw new InvalidOperationException("Insert failed: no rows were affected.");
            }
        }

        public virtual bool Remove(TEntity item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var builder = new SqlBuilder(_tableDefinition.DeleteTemplate);
            BindKeyColumn(item, builder);

            return Connection.Execute(builder.RawSql, builder.Parameters) > 0;
        }

        public virtual TEntity Update(TEntity item)
        {
            ArgumentNullException.ThrowIfNull(item);

            SqlBuilder builder;
            if (RepositoryDiscoveryService.UpdateChangeOnly)
            {
                builder = CreateDeltaBasedUpdate(item);
            }
            else
            {
                builder = new SqlBuilder(_tableDefinition.UpdateTemplate);
                builder.AddParameter(item);
                BindKeyColumn(item, builder);
            }

            var rowsUpdated = Connection.Execute(builder.RawSql, builder.Parameters);
            if (rowsUpdated > 0)
            {
                return FindEntityByPrimaryKey(item);
            }

            throw new InvalidOperationException("Update failed: no rows were affected.");
        }

        public virtual TEntity Find(params object[] orderedKeyValues)
        {
            if (_keyColumns.Length == 0)
            {
                throw new InvalidOperationException($"Table {_tableDefinition.TableName} does not have primary keys.");
            }

            var builder = new SqlBuilder(_tableDefinition.SelectTemplate);
            BuildPrimaryKeyCondition(builder, orderedKeyValues);
            builder.Take(1);

            return Connection.QueryFirstOrDefault<TEntity>(builder.RawSql, builder.Parameters);
        }

        public IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object? parameter = null)
        {
            ArgumentNullException.ThrowIfNull(sqlQuery);

            return Connection.Query<TEntity>(sqlQuery, new DynamicParameter(parameter));
        }

        #endregion

        #region Helpers

        private TEntity FindEntityByPrimaryKey(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var builder = new SqlBuilder(_tableDefinition.SelectTemplate);
            BindKeyColumn(entity, builder);
            builder.Take(1);

            return Connection.QueryFirstOrDefault<TEntity>(builder.RawSql, builder.Parameters)
                   ?? throw new InvalidOperationException("Entity not found after insert/update.");
        }

        private void BindKeyColumn(TEntity entity, SqlBuilder builder)
        {
            foreach (var key in _keyColumns)
            {
                var value = key.Get(entity);
                if (value != null)
                {
                    builder.Where(key.UpdateString);
                    builder.AddParameter(key.Name, value);
                }
                else
                {
                    builder.Where($"{key.EnquotedDbName} IS NULL");
                }
            }
        }

        private void BuildPrimaryKeyCondition(SqlBuilder builder, params object[] orderedKeyValues)
        {
            if (orderedKeyValues.Length != _keyColumns.Length)
            {
                throw new ArgumentException("The number of provided key values does not match the number of primary keys.");
            }

            for (var i = 0; i < _keyColumns.Length; i++)
            {
                var key = _keyColumns[i];
                var value = orderedKeyValues[i];

                if (value != null)
                {
                    builder.Where(key.UpdateString);
                    builder.AddParameter(key.Name, value);
                }
                else
                {
                    builder.Where($"{key.EnquotedDbName} IS NULL");
                }
            }
        }

        private SqlBuilder CreateDeltaBasedUpdate(TEntity item)
        {
            var builder = new SqlBuilder(_tableDefinition.SelectTemplate);
            BindKeyColumn(item, builder);
            builder.Take(1);

            var original = Connection.QueryFirst<TEntity>(builder.RawSql, builder.Parameters);
            var delta = new Delta<TEntity>(item);
            delta.Compare(original, false);

            var changedProperties = delta.GetChangedProperties();
            if (!changedProperties.Any())
            {
                return null!;
            }

            var columns = string.Join(", ", changedProperties
                .Where(c => !c.IsIdentity)
                .Select(c => c.UpdateString));

            builder = new SqlBuilder();
            builder.Template = string.Format(
                RepositoryDiscoveryService.Template.Update,
                RepositoryDiscoveryService.Template.Enquote(_tableDefinition.TableName),
                columns);

            foreach (var prop in changedProperties)
            {
                builder.AddParameter(prop.Name, prop.Get(item));
            }

            BindKeyColumn(original, builder);

            return builder;
        }

        #endregion
    }
}