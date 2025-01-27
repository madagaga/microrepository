using MicroRepository.Caching;
using MicroRepository.Core.Sql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace MicroRepository.Sql
{
    /// <summary>
    /// Provides an enumerable repository for a database entity. Supports lazy-loading of query results.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public partial class EnumerableRepository<TEntity> : IEnumerable<TEntity>
    {
        private readonly string _selectTemplate;
        private SqlBuilder? _internalBuilder;
        private IEnumerable<TEntity>? _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableRepository{TEntity}"/> class.
        /// </summary>
        /// <param name="connection">The database connection to use for queries.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
        internal EnumerableRepository(IDbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection, nameof(connection));

            Connection = connection;
            _selectTemplate = TableDefinitionCache
                .GetTableDefinition(typeof(TEntity))
                .SelectTemplate;
        }

        /// <summary>
        /// Gets the database connection used by the repository.
        /// </summary>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Gets the internal SQL builder used to dynamically construct queries.
        /// </summary>
        internal SqlBuilder InternalBuilder =>
            _internalBuilder ??= new SqlBuilder(_selectTemplate);

        /// <summary>
        /// Retrieves an enumerator for iterating over the query results.
        /// </summary>
        /// <returns>An enumerator over the query results.</returns>
        public IEnumerator<TEntity> GetEnumerator()
        {
            if (_result is null)
            {
                _result = ExecuteQuery();
            }

            return _result.GetEnumerator();
        }

        /// <summary>
        /// Retrieves the enumerator for non-generic iterations.
        /// </summary>
        /// <returns>The non-generic enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Executes the SQL query and retrieves the results.
        /// </summary>
        /// <returns>An enumerable collection of entities from the query.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the query fails.</exception>
        private IEnumerable<TEntity> ExecuteQuery()
        {
            try
            {
                return Connection.Query<TEntity>(
                    InternalBuilder.RawSql,
                    InternalBuilder.Parameters
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to execute the query for EnumerableRepository.", ex);
            }
        }
    }
}