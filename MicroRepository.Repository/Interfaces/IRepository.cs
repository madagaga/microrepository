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

        /// <summary>
        /// Updates element in database <seealso cref="RepositoryDiscoveryService.UpdateChangeOnly"/>
        /// </summary>
        /// <param name="item">item to upload </param>
        /// <returns>element updated from database</returns>
        TEntity Update(TEntity item);

        /// <summary>
        /// Find an element by its primary key
        /// class bust be decorated with KeyAttribute
        /// </summary>
        /// <param name="orderedKeyValues">primary key s</param>
        /// <returns>Found element </returns>
        TEntity Find(params object[] orderedKeyValues);

        /// <summary>
        /// Execute a raw query 
        /// </summary>
        /// <param name="sqlQuery">sql query</param>
        /// <param name="parameter">object parameter</param>
        /// <returns>Found element</returns>
        IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object parameter = null);
    }
}

