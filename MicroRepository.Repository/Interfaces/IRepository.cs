using MicroRepository.Repository.EnumerableEntity;
using MicroRepository.Sql;
using System.Collections.Generic;
using System.Data;

namespace MicroRepository.Repository.Interfaces
{
    public interface IRepository
    {
        IDbConnection Connection { get; }
    }

    public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
    {
        
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
        /// Updates element in database <seealso cref="DbSettings.UpdateChangeOnly"/>
        /// </summary>
        /// <param name="item">item to upload </param>
        /// <returns>element updated from database</returns>
        TEntity Update(TEntity item);

    }
}

