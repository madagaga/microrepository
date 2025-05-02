using MicroRepository.Repository.EnumerableEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository.Interfaces
{
    public interface IReadOnlyRepository<TEntity> : IRepository, IEnumerableRepository<TEntity> 
    {
        /// <summary>
        /// Find an element by its primary key
        /// class bust be decorated with KeyAttribute
        /// </summary>
        /// <param name="orderedKeyValues">primary key s</param>
        /// <returns>Found element </returns>
        TEntity? Find(params object[] orderedKeyValues);

        /// <summary>
        /// Execute a raw query 
        /// </summary>
        /// <param name="sqlQuery">sql query</param>
        /// <param name="parameter">object parameter</param>
        /// <returns>Found element</returns>
        IEnumerable<TEntity> ExecuteQuery(string sqlQuery, object? parameter);
    }
}
