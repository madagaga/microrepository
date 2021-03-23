using MicroRepository.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MicroRepository.Repository.Interfaces
{
    public interface IRepository
    {
        IDbConnection Connection { get; }
    }

    public interface IRepository<TEntity> : IRepository
    {        
        SqlQueryableResult<TEntity> Elements { get; }
        TEntity Add(TEntity item);
        bool Remove(TEntity item);
        TEntity Update(TEntity item);        
        TEntity Find(params object[] orderedKeyValues);
        IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null);            
    }
}

