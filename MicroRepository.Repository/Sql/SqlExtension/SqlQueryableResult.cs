using MicroRepository.Caching;
using MicroRepository.Core.Sql;
using System;
using System.Collections.Generic;
using System.Data;

namespace MicroRepository.Sql
{
    public partial class SqlQueryableResult<TEntity> 
    {
        private string _selectTemplate;
        private object _syncObj = new object();
        internal SqlQueryableResult(IDbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            
            Connection = connection;


            _selectTemplate = TableDefinitionCache.GetTableDefinition(typeof(TEntity)).SelectTemplate;            
            
        }
       
        #region IQueryableRepository Member
        Sql.SqlBuilder _internalBuilder;
        public Sql.SqlBuilder InternalBuilder
        {
            get
            {
                if (_internalBuilder == null)
                    _internalBuilder = new Sql.SqlBuilder(_selectTemplate);
                return _internalBuilder;
            }
        }

        public IDbConnection Connection { get; private set; }
        public bool Enumerated { get; internal set; }
        #endregion

        #region IEnumerable
        IEnumerable<TEntity> _result;
        public IEnumerator<TEntity> GetEnumerator()
        {
            if (_result == null)
                lock (_syncObj)
                {
                    try
                    {
                        _result = Connection.Query<TEntity>(InternalBuilder.RawSql, InternalBuilder.Parameters);
                        Enumerated = true;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Microrepository get enumerator", e);
                    }
                }
            return _result.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion

        
    }
}
