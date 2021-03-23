using MicroRepository.Caching;
using MicroRepository.DynamicParameters;
using MicroRepository.Interfaces;
using MicroRepository.Schema;
using MicroRepository.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository
{
    public partial class Repository<TEntity> : IRepository<TEntity> where TEntity: class
    {

        private TableDefinition _tableDefinition;

        private DataBasePropertyAccessor[] _keyColumns;

        //public Repository() : this(DbFactory.GetConnection()) { }

        private object _syncObj = new object();
        public Repository(IDbConnection connection)
        {   
            Connection = connection;

            Type targetType = typeof(TEntity);

            _tableDefinition = TableDefinitionCache.GetTableDefinition(targetType);

            this._keyColumns = _tableDefinition.Members.Values.Where(c => c.IsPrimaryKey).ToArray();            
        }


#region IRepository 
                
        public IDbConnection Connection { get; private set; }

        public SqlQueryableResult<TEntity> Elements
        {
            get { return new Sql.SqlQueryableResult<TEntity>(Connection); }
        }

        public virtual TEntity Add(TEntity item)
        {            
            SqlBuilder builder = new SqlBuilder(_tableDefinition.InsertTemplate);
            builder.AddParameter(item);
            lock (_syncObj)
            {
                // if has identity primari key
                if (_tableDefinition.HasIdentity)
                {
                    int res = Connection.ExecuteScalar<int>(builder.RawSql, builder.Parameters);
                    if (res > 0)
                        return Find(res);
                }
                else
                {
                    int lc = Connection.Execute(builder.RawSql, builder.Parameters);
                    if(lc>0)
                    {
                        if (_keyColumns.Length > 0)
                        {
                            builder = new SqlBuilder(_tableDefinition.SelectTemplate);
                            bindKeyColumn(item, builder);
                            builder.Take(1);
                            return Connection.QueryFirst<TEntity>(builder.RawSql, builder.Parameters);
                        }
                        else return item;
                    }
                }


                throw new Exception("Insert failed");
            }
        }

        public virtual bool Remove(TEntity item)
        {
            SqlBuilder builder = new SqlBuilder(_tableDefinition.DeleteTemplate);
            bindKeyColumn(item, builder);
            lock (_syncObj)            
            return Connection.Execute(builder.RawSql, builder.Parameters) != 0;
        }

        public virtual TEntity Update(TEntity item)
        {
            SqlBuilder builder = null; 
            lock (_syncObj)
            {
                if (DbFactory.UpdateChangeOnly)
                {
                    //delta 
                    builder = new SqlBuilder(_tableDefinition.SelectTemplate);
                    bindKeyColumn(item, builder);
                    builder.Take(1);
                    TEntity original = Connection.QueryFirst<TEntity>(builder.RawSql, builder.Parameters);
                    Delta<TEntity> delta = new Delta<TEntity>(item);
                    delta.Compare(original, false);
                    DataBasePropertyAccessor[] changedProps = delta.GetChangedProperties();
                    if (changedProps.Length == 0)
                        return item;

                    builder = new SqlBuilder();
                    string[] columns = changedProps.Where(c => !c.IsIdentity).Select(c => c.UpdateString).ToArray();

                    builder.Template = string.Format(DbFactory.Template.Update, DbFactory.Template.Enquote(_tableDefinition.TableName), string.Join(", ", columns));
                    foreach (DataBasePropertyAccessor prop in changedProps)
                        builder.AddParameter(prop.Name, prop.Get(item));

                    bindKeyColumn(original, builder);
                }
                else
                {
                    
                    builder = new SqlBuilder(_tableDefinition.UpdateTemplate);
                    builder.AddParameter(item);
                    bindKeyColumn(item, builder);
                }

                if (Connection.Execute(builder.RawSql, builder.Parameters) != 0)
                {
                    builder = new SqlBuilder(_tableDefinition.SelectTemplate);
                    bindKeyColumn(item, builder);
                    builder.Take(1);
                    return Connection.QueryFirst<TEntity>(builder.RawSql, builder.Parameters);
                }
                return default(TEntity);
            }
        }

        
        public virtual TEntity Find(params object[] orderedKeyValues)
        {
            if (_keyColumns.Length == 0)
                throw new Exception("Table " + _tableDefinition.TableName + " does not have primary keys");
            SqlBuilder builder = new SqlBuilder();
            int i = 0;
            foreach (object key in orderedKeyValues)
            {
                if (key != null)
                {
                    builder.Where(_keyColumns[i].UpdateString);
                    builder.AddParameter(_keyColumns[i].Name, key);
                }
                else
                    builder.Where(string.Format("{0} IS NULL", _keyColumns[i].EnquotedDbName));
                i++;

            }
            builder.Take(1);
            builder.Template = _tableDefinition.SelectTemplate;
            lock (_syncObj)
                return Connection.QueryFirstOrDefault<TEntity>(builder.RawSql, builder.Parameters);
        }

        public IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null)
        {
            lock (_syncObj)
                return Connection.Query<TEntity>(sqlQuery, new DynamicParameter(parameter));
        }
        

#endregion


#region helpers        

        void bindKeyColumn(TEntity item, SqlBuilder builder)
        {
            object value = null;
            foreach (DataBasePropertyAccessor key in _keyColumns)
            {
                value = key.Get(item);
                if (value != null)
                {
                    builder.Where(key.UpdateString);
                    builder.AddParameter(key.Name, value);
                }
                else
                    builder.Where(string.Format("{0} IS NULL", key.EnquotedDbName));

            }
        }

#endregion
        
    }
}

