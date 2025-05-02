using MicroRepository.Caching;
using MicroRepository.Core.Sql;
using MicroRepository.Repository;
using MicroRepository.Repository.EnumerableEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MicroRepository.Sql
{
    public static class EnumerableRepositoryExtensions
    {
        private static void CheckExecution<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            if (repo.Enumerated)
                throw new InvalidOperationException("Sql query has already been executed. You can not add new clauses");
        }

        public static IEnumerableRepository<TEntity> AndRawSql<TEntity>(this IEnumerableRepository<TEntity> repo, string sql)
        {
            repo.CheckExecution();
            repo.InternalBuilder.Where(sql);
            return repo;
        }

        public static IEnumerableRepository<TEntity> OrRawSql<TEntity>(this IEnumerableRepository<TEntity> repo, string sql)
        {
            repo.CheckExecution();
            repo.InternalBuilder.OrWhere(sql);
            return repo;
        }

        public static IEnumerableRepository<TEntity> LeftJoin<TEntity, TJoin>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TJoin, bool>> selector)
        {
            repo.InternalBuilder.LeftJoin(JoinImpl(repo, selector));
            return repo;
        }

        public static IEnumerableRepository<TEntity> InnerJoin<TEntity, TJoin>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TJoin, bool>> selector)
        {
            repo.InternalBuilder.InnerJoin(JoinImpl(repo, selector));
            return repo;
        }

        private static string JoinImpl<TEntity, TJoin>(IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TJoin, bool>> selector)
        {
            repo.CheckExecution();
            List<QueryParameter> queryProperties = new List<QueryParameter>();
            ExpressionParser.ParseExpression(selector.Body, ref queryProperties);
            var joinTable = TableDefinitionCache.GetTableDefinition(typeof(TJoin));
            StringBuilder sqlchunk = new StringBuilder($"{joinTable.DBName} AS {joinTable.Alias} ON");

            foreach (QueryParameter item in queryProperties)
            {
                sqlchunk.Append($"{item.LinkingOperator} ");
                if (item.PropertyValue != null)
                {
                    if (!string.IsNullOrEmpty(item.PropertyFormat))
                    {
                        if (item.PropertyFormat == "[table]")
                        {
                            sqlchunk.AppendFormat("{0} {1} {2} ", item.PropertyName, item.QueryOperator, item.PropertyValue);
                            continue;
                        }
                        else
                        {
                            sqlchunk.AppendFormat(item.PropertyFormat, repo.InternalBuilder.Parameters.Count);
                            sqlchunk.AppendFormat("{0} {1} ", item.QueryOperator, item.PropertyValue);
                        }
                    }
                    else
                        sqlchunk.AppendFormat("{0} {1} @p{2} ", item.PropertyName, item.QueryOperator, repo.InternalBuilder.Parameters.Count);
                    repo.InternalBuilder.AddParametersWithCount(item.PropertyValue);
                }
                else
                    sqlchunk.AppendFormat("{0} {1} ", item.PropertyName, item.QueryOperator);
            }
            return sqlchunk.ToString();
        }

        public static IEnumerable<TKey> Select<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,Expression<Func<TEntity, TKey>> selector)
        {
            if (selector.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Selector must be a member expression", nameof(selector));
            }

            var table = TableDefinitionCache.GetTableDefinition(typeof(TEntity));

            if (!table.Members.ContainsKey(memberExpression.Member.Name))
            {
                throw new ArgumentException($"Member {memberExpression.Member.Name} not found in table definition", nameof(selector));
            }

            string column = table.Members[memberExpression.Member.Name].FullDbName;
            repo.InternalBuilder.Template = string.Format(DbSettings.Template.Select, column, $"{table.DBName} AS {table.Alias}");

            var result = repo.Connection.Query<TKey>(repo.InternalBuilder.RawSql, repo.InternalBuilder.Parameters);

            return result ?? Enumerable.Empty<TKey>();
        }

        public static IEnumerableRepository<TEntity> In<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
    Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {
            if (selector.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Selector must be a member expression", nameof(selector));
            }
            repo.InternalBuilder.Where(InImpl(repo, memberExpression, search, "IN"));
            return repo;
        }

        public static IEnumerableRepository<TEntity> NotIn<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {
            if (selector.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Selector must be a member expression", nameof(selector));
            }
            repo.InternalBuilder.Where(InImpl(repo, memberExpression, search, "NOT IN"));
            return repo;
        }

        public static IEnumerableRepository<TEntity> OrIn<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {
            if (selector.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Selector must be a member expression", nameof(selector));
            }
            repo.InternalBuilder.OrWhere(InImpl(repo, memberExpression, search, "IN"));
            return repo;
        }

        public static IEnumerableRepository<TEntity> OrNotIn<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {
            if (selector.Body is not MemberExpression memberExpression)
            {
                throw new ArgumentException("Selector must be a member expression", nameof(selector));
            }
            repo.InternalBuilder.OrWhere(InImpl(repo, memberExpression, search, "NOT IN"));
            return repo;
        }

        private static string InImpl<TEntity, TKey>(IEnumerableRepository<TEntity> repo,
            MemberExpression memberExpression, IEnumerable<TKey> search, string queryOperator)
        {
            if (memberExpression.Member.DeclaringType == null)
            {
                throw new ArgumentException("Member declaring type cannot be null", nameof(memberExpression));
            }

            if (search == null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            repo.CheckExecution();

            var properties = TableDefinitionCache.GetPropertiesDictionary(memberExpression.Member.DeclaringType);

            if (!properties.ContainsKey(memberExpression.Member.Name))
            {
                throw new ArgumentException($"Property {memberExpression.Member.Name} not found in table definition", nameof(memberExpression));
            }

            string column = properties[memberExpression.Member.Name].FullDbName;
            List<string> indexes = new List<string>();

            foreach (var item in search)
            {
                indexes.Add($"@p{repo.InternalBuilder.Parameters.Count}");
                repo.InternalBuilder.AddParametersWithCount(item);
            }

            return $"{column} {queryOperator} ({string.Join(", ", indexes)})";
        }

        public static bool Any<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            return repo.Count() != 0;
        }

        public static bool Any<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>> predicate)
        {
            return repo.Count(predicate) != 0;
        }

        public static int Count<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return Count(repo, null);
        }

        public static int Count<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.CheckExecution();
            if (predicate != null)
                repo.Where(predicate);
            repo.InternalBuilder.Template = TableDefinitionCache.GetTableDefinition(typeof(TEntity)).CountTemplate;            
            return repo.Connection.ExecuteScalar<int>(repo.InternalBuilder.RawSql, repo.InternalBuilder.Parameters);
        }

        public static IEnumerableRepository<TEntity> Distinct<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            repo.InternalBuilder.Distinct();
            return repo;
        }

        public static TEntity? FirstOrDefault<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return FirstOrDefault(repo, null);
        }

        public static TEntity? FirstOrDefault<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.CheckExecution();
            repo.InternalBuilder.Take(1);
            if (predicate != null)
                Where(repo, predicate);
            return repo.ToList().FirstOrDefault();
        }

        public static IEnumerableRepository<TEntity> GroupBy<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> keySelector)
        {
            repo.CheckExecution();
            MemberExpression body = (MemberExpression)keySelector.Body;
            string column = TableDefinitionCache.GetPropertiesDictionary(typeof(TEntity))[body.Member.Name].FullDbName;
            repo.InternalBuilder.GroupBy(column);
            return repo;
        }

        public static TEntity Last<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return Last(repo, null);
        }

        public static TEntity Last<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.CheckExecution();
            if (predicate != null)
                Where(repo, predicate);
            return ((IEnumerable<TEntity>)repo).Last();
        }

        public static TEntity? LastOrDefault<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return LastOrDefault(repo, null);
        }

        public static TEntity? LastOrDefault<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.CheckExecution();
            if (predicate != null)
                Where(repo, predicate);
            return ((IEnumerable<TEntity>)repo).LastOrDefault();
        }

        public static long LongCount<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return (long)Count(repo);
        }

        public static long LongCount<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>> predicate)
        {
            repo.CheckExecution();
            return (long)Count(repo, predicate);
        }

        public static IEnumerableRepository<TEntity> OrderBy<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> keySelector)
        {
            repo.CheckExecution();
            MemberExpression body = (MemberExpression)keySelector.Body;
            string column = TableDefinitionCache.GetPropertiesDictionary(typeof(TEntity))[body.Member.Name].FullDbName;
            repo.InternalBuilder.OrderBy(column);
            return repo;
        }

        public static IEnumerableRepository<TEntity> OrderByDescending<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> keySelector)
        {
            repo.CheckExecution();
            MemberExpression body = (MemberExpression)keySelector.Body;
            string column = TableDefinitionCache.GetPropertiesDictionary(typeof(TEntity))[body.Member.Name].FullDbName;
            repo.InternalBuilder.OrderBy(column + " DESC");
            return repo;
        }

        public static TEntity Single<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return Single(repo, null);
        }

        public static TEntity Single<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.CheckExecution();
            if (predicate != null)
                Where(repo, predicate);
            return ((IEnumerable<TEntity>)repo).Single();
        }

        public static TEntity SingleOrDefault<TEntity>(this IEnumerableRepository<TEntity> repo)
        {
            repo.CheckExecution();
            return SingleOrDefault(repo, null);
        }

        public static TEntity SingleOrDefault<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.CheckExecution();
            if (predicate != null)
                Where(repo, predicate);
            return ((IEnumerable<TEntity>)repo).Single();
        }

        public static IEnumerableRepository<TEntity> Skip<TEntity>(this IEnumerableRepository<TEntity> repo, int count)
        {
            repo.InternalBuilder.Skip(count);
            return repo;
        }

        public static IEnumerableRepository<TEntity> Take<TEntity>(this IEnumerableRepository<TEntity> repo, int count)
        {
            repo.InternalBuilder.Take(count);
            return repo;
        }

        public static IEnumerableRepository<TEntity> TakeWhile<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>> predicate)
        {
            repo.CheckExecution();
            return Where(repo, predicate);
        }

        public static IEnumerableRepository<TEntity> ThenBy<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> keySelector)
        {
            repo.CheckExecution();
            return OrderBy(repo, keySelector);
        }

        public static IEnumerableRepository<TEntity> ThenByDescending<TEntity, TKey>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, TKey>> keySelector)
        {
            repo.CheckExecution();
            return OrderByDescending(repo, keySelector);
        }

        public static IEnumerableRepository<TEntity> Where<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>> predicate)
        {
            repo.InternalBuilder.Where($"({WhereImpl(repo, predicate)})");
            return repo;
        }

        public static IEnumerableRepository<TEntity> OrWhere<TEntity>(this IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            repo.InternalBuilder.OrWhere($"({WhereImpl(repo, predicate)})", null);
            return repo;
        }

        private static string WhereImpl<TEntity>(IEnumerableRepository<TEntity> repo,
            Expression<Func<TEntity, bool>>? predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (predicate.Body == null)
            {
                throw new ArgumentException("Predicate body cannot be null", nameof(predicate));
            }

            repo.CheckExecution();
            List<QueryParameter> queryProperties = new List<QueryParameter>();
            ExpressionParser.ParseExpression(predicate.Body, ref queryProperties);
            StringBuilder sqlchunk = new StringBuilder();

            foreach (QueryParameter item in queryProperties)
            {
                sqlchunk.Append($"{item.LinkingOperator} ");
                if (item.PropertyValue != null)
                {
                    if (!string.IsNullOrEmpty(item.PropertyFormat))
                    {
                        sqlchunk.AppendFormat(item.PropertyFormat, repo.InternalBuilder.Parameters.Count);
                        sqlchunk.AppendFormat("{0} {1} ", item.QueryOperator, item.PropertyValue);
                    }
                    else
                        sqlchunk.AppendFormat("{0} {1} @p{2} ", item.PropertyName, item.QueryOperator, repo.InternalBuilder.Parameters.Count);
                    repo.InternalBuilder.AddParametersWithCount(item.PropertyValue);
                }
                else
                    sqlchunk.AppendFormat("{0} {1} ", item.PropertyName, item.QueryOperator);
            }
            return sqlchunk.ToString();
        }
    }
}
