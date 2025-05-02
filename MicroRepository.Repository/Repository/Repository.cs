using MicroRepository.Core.Sql;
using MicroRepository.Repository.Interfaces;
using MicroRepository.Repository.Repository;
using MicroRepository.Schema;
using MicroRepository.Sql;
using System;
using System.Data;
using System.Linq;

namespace MicroRepository.Repository
{
    public class Repository<TEntity> : ReadOnlyRepository<TEntity>, IRepository<TEntity> where TEntity : class
    {

        public Repository(IDbConnection connection): base(connection)
        {
            
        }

        #region IRepository
        
        public virtual TEntity Add(TEntity item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var builder = new SqlBuilder(TableDefinition.InsertTemplate);
            builder.AddParameter(item);

            if (TableDefinition.HasAutoId)
            {
                // retrieve primarykey
                ColumnDescriptor cd = KeyColumns.Single(c=>c.IsAutoId);

                object? raw = Connection.ExecuteScalar(builder.RawSql, builder.Parameters);
                if(raw == null)
                    throw new InvalidOperationException("Insert failed: no identity key returned.");


                //object id = Convert.ChangeType(raw, cd.Type);                
                TEntity? result = Find(raw);
                if (result == null)
                    throw new NullReferenceException();
                return result;
            }
            else
            {
                var rowsInserted = Connection.Execute(builder.RawSql, builder.Parameters);
                if (rowsInserted > 0)
                {
                    if (KeyColumns.Any())
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

            var builder = new SqlBuilder(TableDefinition.DeleteTemplate);
            BindKeyColumn(item, builder);

            return Connection.Execute(builder.RawSql, builder.Parameters) > 0;
        }

        public virtual TEntity Update(TEntity item)
        {
            ArgumentNullException.ThrowIfNull(item);

            SqlBuilder builder = new SqlBuilder(TableDefinition.UpdateTemplate);
            builder.AddParameter(item);
            BindKeyColumn(item, builder);


            var rowsUpdated = Connection.Execute(builder.RawSql, builder.Parameters);
            if (rowsUpdated > 0)
            {
                return FindEntityByPrimaryKey(item);
            }

            throw new InvalidOperationException("Update failed: no rows were affected.");
        }

       
        #endregion

    }
}