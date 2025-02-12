using MicroRepository.Caching;
using MicroRepository.Core.Sql;
using MicroRepository.Repository;
using MicroRepository.Repository.EnumerableEntity;
using MicroRepository.Repository.Paging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MicroRepository.Sql
{
    /// <summary>
    /// Provides an enumerable repository for a database entity. Supports lazy-loading of query results.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class EnumerableRepository<TEntity> : IEnumerableRepository<TEntity>
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
        public SqlBuilder InternalBuilder =>
            _internalBuilder ??= new SqlBuilder(_selectTemplate);

        public bool Enumerated => _result != null;

        
        public List<TEntity> ToList()
        {
            return AsIEnumerable().ToList();
        }

        public IPagedList<TEntity> ToPagedList(int page, int resultPerPage)
        {
            string countSql = $"SELECT COUNT(*) FROM ({InternalBuilder.RawSql}) AS subquery";
            
            string pagedSql = string.Concat(InternalBuilder.RawSql, " ", string.Format(DbSettings.Template.Take, " @ResultPerPage "), string.Format(DbSettings.Template.Skip, " @Offset"));

           
            
            InternalBuilder.Parameters.Add("Offset", (page - 1) * resultPerPage);
            InternalBuilder.Parameters.Add("ResultPerPage", resultPerPage);

            int totalResults = Connection.ExecuteScalar<int>(countSql, InternalBuilder.Parameters);
            IEnumerable<TEntity> items = Connection.Query<TEntity>(pagedSql, InternalBuilder.Parameters);

            return new PagedList<TEntity>(items, page, totalResults, resultPerPage);
        }

        /// <summary>
        /// Executes the SQL query and retrieves the results.
        /// </summary>
        /// <returns>An enumerable collection of entities from the query.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the query fails.</exception>
        public IEnumerable<TEntity> AsIEnumerable()
        {
            if (_result is null)
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
                finally
                {

                    _internalBuilder = null;
                }
            }

            return _result;
        }
            
    }
}