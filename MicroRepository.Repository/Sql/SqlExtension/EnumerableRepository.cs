using MicroRepository.Caching;
using MicroRepository.Core.Sql;
using System;
using System.Collections.Generic;
using System.Data;

namespace MicroRepository.Sql
{
    public partial class EnumerableRepository<TEntity>
    {
        private readonly string _selectTemplate;
        private readonly object _syncObj = new object();
        internal EnumerableRepository(IDbConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException("connection");
            _selectTemplate = TableDefinitionCache.GetTableDefinition(typeof(TEntity)).SelectTemplate;

        }

        #region IQueryableRepository Member
        Sql.SqlBuilder _internalBuilder;
        internal Sql.SqlBuilder InternalBuilder
        {
            get
            {
                if (_internalBuilder == null)
                    _internalBuilder = new Sql.SqlBuilder(_selectTemplate);
                return _internalBuilder;
            }
        }

        public IDbConnection Connection { get; private set; }
        internal bool Enumerated { get; set; }
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
