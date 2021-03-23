using MicroRepository.Sql;
using System.Collections.Generic;
using System.Data;

namespace MicroRepository.Repository.Interfaces
{
    public interface IRepository
    {
        IDbConnection Connection { get; }
    }

    public interface IRepository<TEntity> : IRepository
    {
        /// <summary>
        /// Elements - data queryable 
        /// </summary>
        EnumerableRepository<TEntity> Elements { get; }

        /// <summary>
        /// Add an element to database
        /// </summary>
        /// <param name="item">element to be added </param>
        /// <returns></returns>
        TEntity Add(TEntity item);

        /// <summary>
        /// Removes element from database 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool Remove(TEntity item);
        TEntity Update(TEntity item);
        TEntity Find(params object[] orderedKeyValues);
        IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null);
    }
}

