using MicroRepository.Caching;
using MicroRepository.Core.Sql;
using MicroRepository.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MicroRepository.Sql
{
    public partial class EnumerableRepository<TEntity> : IEnumerable<TEntity>
    {

        void CheckExecution()
        {
            if (this.Enumerated)
                throw new InvalidOperationException("Sql query has already been executed. You can not add new clauses");
        }

        /// <summary>
        /// Add Raw SQL and returns a <see cref="EnumerableRepository{T}"/>
        /// </summary>
        /// <param name="sql">sql to execute</param>
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public EnumerableRepository<TEntity> AndRawSql(string sql)
        {
            CheckExecution();
            this.InternalBuilder.Where(sql);
            return this;
        }

        /// <summary>
        /// Add Raw SQL and returns a <see cref="EnumerableRepository{T}"/>
        /// </summary>
        /// <param name="sql">sql to execute</param>
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public EnumerableRepository<TEntity> OrRawSql(string sql)
        {
            CheckExecution();
            this.InternalBuilder.OrWhere(sql);
            return this;
        }

        public EnumerableRepository<TEntity> LeftJoin<TJoin>(Expression<Func<TEntity, TJoin, bool>> selector)
        {
            List<QueryParameter> queryProperties = new List<QueryParameter>();
            this.InternalBuilder.LeftJoin(JoinImpl(selector));
            return this;
        }

        public EnumerableRepository<TEntity> InnerJoin<TJoin>(Expression<Func<TEntity, TJoin, bool>> selector)
        {
            List<QueryParameter> queryProperties = new List<QueryParameter>();
            this.InternalBuilder.InnerJoin(JoinImpl(selector));
            return this;
        }

        private string JoinImpl<TJoin>(Expression<Func<TEntity, TJoin, bool>> selector)
        {
            CheckExecution();
            List<QueryParameter> queryProperties = new List<QueryParameter>();
            ExpressionParser.ParseExpression(selector.Body, ExpressionType.Default, ref queryProperties);

            
            var joinTable = TableDefinitionCache.GetTableDefinition(typeof(TJoin));
            StringBuilder sqlchunk = new StringBuilder($"{RepositoryDiscoveryService.Template.Enquote(joinTable.TableName)} ON");
            foreach (QueryParameter item in queryProperties)
            {
                sqlchunk.Append($"{item.LinkingOperator} ");
                if (item.PropertyValue != null)
                    sqlchunk.AppendFormat("{0} {1} {2} ", item.PropertyName, item.QueryOperator, item.PropertyValue);
                else
                    sqlchunk.AppendFormat("{0} {1}", item.PropertyName, item.QueryOperator);
            }

            return sqlchunk.ToString();
        }

        /// <summary>
        /// Select specific property
        /// </summary>
        /// <param name="selector">column selector </param>        
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public IEnumerable<TKey> Select<TKey>(Expression<Func<TEntity, TKey>> selector)
        {
            MemberExpression memberExpression = selector.Body as MemberExpression;
            var table = TableDefinitionCache.GetTableDefinition(typeof(TEntity));
            string column = table.Members[memberExpression.Member.Name].EnquotedFullName;
            InternalBuilder.Template = string.Format(RepositoryDiscoveryService.Template.Select, column, table.SelectTableName);

            ((EnumerableRepository<TEntity>)this).Enumerated = true;
            return Connection.Query<TKey>(InternalBuilder.RawSql, InternalBuilder.Parameters);            
        }


        /// <summary>
        /// Add a Where in clause
        /// </summary>
        /// <param name="selector">column selector </param>
        /// <param name="search">array of data </param>
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public EnumerableRepository<TEntity> In<TKey>(Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {            
            this.InternalBuilder.Where(InImpl(selector.Body as MemberExpression, search, "IN"));
            return this;
        }

        public EnumerableRepository<TEntity> NotIn<TKey>(Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {   
            this.InternalBuilder.Where(InImpl(selector.Body as MemberExpression, search, "NOT IN"));            
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="search"></param>
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public EnumerableRepository<TEntity> OrIn<TKey>(Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {
            this.InternalBuilder.OrWhere(InImpl(selector.Body as MemberExpression, search, "IN"));            
            return this;
        }

        public EnumerableRepository<TEntity> OrNotIn<TKey>(Expression<Func<TEntity, TKey>> selector, IEnumerable<TKey> search)
        {
            this.InternalBuilder.OrWhere(InImpl(selector.Body as MemberExpression, search, "NOT IN"));
            return this;
        }

        private string InImpl<TKey>(MemberExpression memberExpression, IEnumerable<TKey> search, string queryOperator)        
        {
            CheckExecution();
            string column = TableDefinitionCache.GetPropertiesDictionary(memberExpression.Member.DeclaringType)[memberExpression.Member.Name].EnquotedDbName;
            List<string> indexes = new List<string>();
            foreach (object o in search)
            {
                indexes.Add($"@p{ this.InternalBuilder.Parameters.Count}");
                this.InternalBuilder.AddParametersWithCount(o);
            }

            return $"{column} {queryOperator} ({string.Join(", ", indexes)})";
        }




        /// <summary>
        /// Add an equivalent of exists 
        /// </summary>
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public bool Any()
        {
            return this.Count() != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns><see cref="EnumerableRepository{T}"/></returns>
        public bool Any(Expression<Func<TEntity, bool>> predicate)
        {
            return this.Count(predicate) != 0;
        }


        //
        // Summary:
        //     Returns the number of elements in a sequence.
        //
        // Parameters:
        //   source:
        //     A sequence that contains elements to be counted.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The number of elements in the input sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        //
        //   System.OverflowException:
        //     The number of elements in source is larger than System.Int32.MaxValue.
        ///
        public int Count()
        {
            CheckExecution();
            return Count(null);

        }
        //
        // Summary:
        //     Returns a number that represents how many elements in the specified sequence
        //     satisfy a condition.
        //
        // Parameters:
        //   source:
        //     A sequence that contains elements to be tested and counted.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     A number that represents how many elements in the sequence satisfy the condition
        //     in the predicate function.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        //
        //   System.OverflowException:
        //     The number of elements in source is larger than System.Int32.MaxValue.
        public int Count(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            if (predicate != null)
                this.Where(predicate);

            InternalBuilder.Template = TableDefinitionCache.GetTableDefinition(typeof(TEntity)).CountTemplate;
            ((EnumerableRepository<TEntity>)this).Enumerated = true;
            return Connection.ExecuteScalar<int>(InternalBuilder.RawSql, InternalBuilder.Parameters);
        }

        //
        // Summary:
        //     Returns distinct elements from a sequence by using the default equality comparer
        //     to compare values.
        //
        // Parameters:
        //   source:
        //     The sequence to remove duplicate elements from.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains distinct elements
        //     from the source sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        public EnumerableRepository<TEntity> Distinct()
        {
            CheckExecution();
            InternalBuilder.Distinct();
            return this;
        }



        //
        // Summary:
        //     Returns the first element of a sequence, or a default value if the sequence
        //     contains no elements.
        //
        // Parameters:
        //   source:
        //     The System.Collections.Generic.IEnumerable<TEntity> to return the first element
        //     of.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     default(TEntity) if source is empty; otherwise, the first element in source.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        public TEntity FirstOrDefault()
        {
            CheckExecution();
            return FirstOrDefault(null);
        }
        //
        // Summary:
        //     Returns the first element of the sequence that satisfies a condition or a
        //     default value if no such element is found.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return an element from.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     default(TEntity) if source is empty or if no element passes the test specified
        //     by predicate; otherwise, the first element in source that passes the test
        //     specified by predicate.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            InternalBuilder.Take(1);
            if (predicate != null)
                Where(predicate);
            return ((IEnumerable<TEntity>)this).FirstOrDefault();
        }
        //
        // Summary:
        //     Groups the elements of a sequence according to a specified key selector function.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> whose elements to group.
        //
        //   keySelector:
        //     A function to extract the key for each element.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        //   TKey:
        //     The type of the key returned by keySelector.
        //
        // Returns:
        //     An IEnumerable<IGrouping<TKey, TEntity>> in C# or IEnumerable(Of IGrouping(Of
        //     TKey, TEntity)) in Visual Basic where each System.Linq.IGrouping<TKey,TElement>
        //     object contains a sequence of objects and a key.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or keySelector is null.
        public EnumerableRepository<TEntity> GroupBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            CheckExecution();
            MemberExpression body = (MemberExpression)keySelector.Body;
            string column = TableDefinitionCache.GetPropertiesDictionary(typeof(TEntity))[body.Member.Name].EnquotedDbName;
            InternalBuilder.GroupBy(column);
            return this;
        }


        //
        // Summary:
        //     Returns the last element of a sequence.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return the last element of.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The value at the last position in the source sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        //
        //   System.InvalidOperationException:
        //     The source sequence is empty.
        public TEntity Last()
        {
            CheckExecution();
            return Last(null);
        }
        //
        // Summary:
        //     Returns the last element of a sequence that satisfies a specified condition.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return an element from.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The last element in the sequence that passes the test in the specified predicate
        //     function.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        //
        //   System.InvalidOperationException:
        //     No element satisfies the condition in predicate.-or-The source sequence is
        //     empty.
        public TEntity Last(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            if (predicate != null)
                Where(predicate);
            return ((IEnumerable<TEntity>)this).Last();
        }
        //
        // Summary:
        //     Returns the last element of a sequence, or a default value if the sequence
        //     contains no elements.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return the last element of.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     default(TEntity) if the source sequence is empty; otherwise, the last element
        //     in the System.Collections.Generic.IEnumerable<TEntity>.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        public TEntity LastOrDefault()
        {
            CheckExecution();
            return LastOrDefault(null);
        }
        //
        // Summary:
        //     Returns the last element of a sequence that satisfies a condition or a default
        //     value if no such element is found.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return an element from.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     default(TEntity) if the sequence is empty or if no elements pass the test
        //     in the predicate function; otherwise, the last element that passes the test
        //     in the predicate function.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        public TEntity LastOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            if (predicate != null)
                Where(predicate);
            return ((IEnumerable<TEntity>)this).LastOrDefault();
        }

        //
        // Summary:
        //     Returns an System.Int64 that represents the total number of elements in a
        //     sequence.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains the elements to
        //     be counted.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The number of elements in the source sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        //
        //   System.OverflowException:
        //     The number of elements exceeds System.Int64.MaxValue.
        public long LongCount()
        {
            CheckExecution();
            return (long)Count();
        }
        //
        // Summary:
        //     Returns an System.Int64 that represents how many elements in a sequence satisfy
        //     a condition.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains the elements to
        //     be counted.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     A number that represents how many elements in the sequence satisfy the condition
        //     in the predicate function.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        //
        //   System.OverflowException:
        //     The number of matching elements exceeds System.Int64.MaxValue.
        public long LongCount(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            return (long)Count(predicate);
        }


        //
        // Summary:
        //     Filters the elements of an System.Collections.IEnumerable based on a specified
        //     type.
        //
        // Parameters:
        //   source:
        //     The System.Collections.IEnumerable whose elements to filter.
        //
        // Type parameters:
        //   TResult:
        //     The type to filter the elements of the sequence on.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains elements from
        //     the input sequence of type TResult.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        //public  IEnumerable<TResult> OfType<TResult>(this IQueryablethis source);
        //
        // Summary:
        //     Sorts the elements of a sequence in ascending order according to a key.
        //
        // Parameters:
        //   source:
        //     A sequence of values to order.
        //
        //   keySelector:
        //     A function to extract a key from an element.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        //   TKey:
        //     The type of the key returned by keySelector.
        //
        // Returns:
        //     An System.Linq.IOrderedEnumerable<TElement> whose elements are sorted according
        //     to a key.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or keySelector is null.
        public EnumerableRepository<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            CheckExecution();
            MemberExpression body = (MemberExpression)keySelector.Body;
            string column = TableDefinitionCache.GetPropertiesDictionary(typeof(TEntity))[body.Member.Name].EnquotedDbName;
            InternalBuilder.OrderBy(column);
            return this;
        }

        //
        // Summary:
        //     Sorts the elements of a sequence in descending order according to a key.
        //
        // Parameters:
        //   source:
        //     A sequence of values to order.
        //
        //   keySelector:
        //     A function to extract a key from an element.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        //   TKey:
        //     The type of the key returned by keySelector.
        //
        // Returns:
        //     An System.Linq.IOrderedEnumerable<TElement> whose elements are sorted in
        //     descending order according to a key.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or keySelector is null.
        public EnumerableRepository<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            CheckExecution();
            MemberExpression body = (MemberExpression)keySelector.Body;
            string column = TableDefinitionCache.GetPropertiesDictionary(typeof(TEntity))[body.Member.Name].EnquotedDbName;
            InternalBuilder.OrderBy(column + " DESC");
            return this;
        }


        //
        // Summary:
        //     Returns the only element of a sequence, and throws an exception if there
        //     is not exactly one element in the sequence.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return the single element
        //     of.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The single element of the input sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        //
        //   System.InvalidOperationException:
        //     The input sequence contains more than one element.-or-The input sequence
        //     is empty.
        public TEntity Single()
        {
            CheckExecution();
            return Single(null);
        }
        //
        // Summary:
        //     Returns the only element of a sequence that satisfies a specified condition,
        //     and throws an exception if more than one such element exists.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return a single element from.
        //
        //   predicate:
        //     A function to test an element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The single element of the input sequence that satisfies a condition.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        //
        //   System.InvalidOperationException:
        //     No element satisfies the condition in predicate.-or-More than one element
        //     satisfies the condition in predicate.-or-The source sequence is empty.
        public TEntity Single(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            if (predicate != null)
                Where(predicate);
            return ((IEnumerable<TEntity>)this).Single();
        }
        //
        // Summary:
        //     Returns the only element of a sequence, or a default value if the sequence
        //     is empty; this method throws an exception if there is more than one element
        //     in the sequence.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return the single element
        //     of.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The single element of the input sequence, or default(TEntity) if the sequence
        //     contains no elements.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        //
        //   System.InvalidOperationException:
        //     The input sequence contains more than one element.
        public TEntity SingleOrDefault()
        {
            CheckExecution();
            return SingleOrDefault(null);
        }
        //
        // Summary:
        //     Returns the only element of a sequence that satisfies a specified condition
        //     or a default value if no such element exists; this method throws an exception
        //     if more than one element satisfies the condition.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return a single element from.
        //
        //   predicate:
        //     A function to test an element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     The single element of the input sequence that satisfies the condition, or
        //     default(TEntity) if no such element is found.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            if (predicate != null)
                Where(predicate);
            return ((IEnumerable<TEntity>)this).Single();
        }

        //
        // Summary:
        //     Bypasses a specified number of elements in a sequence and then returns the
        //     remaining elements.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to return elements from.
        //
        //   count:
        //     The number of elements to skip before returning the remaining elements.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains the elements that
        //     occur after the specified index in the input sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        public EnumerableRepository<TEntity> Skip(int count)
        {
            InternalBuilder.Skip(count);
            return this;
        }


        //
        // Summary:
        //     Returns a specified number of contiguous elements from the start of a sequence.
        //
        // Parameters:
        //   source:
        //     The sequence to return elements from.
        //
        //   count:
        //     The number of elements to return.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains the specified
        //     number of elements from the start of the input sequence.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source is null.
        public EnumerableRepository<TEntity> Take(int count)
        {
            InternalBuilder.Take(count);
            return this;
        }
        //
        // Summary:
        //     Returns elements from a sequence as long as a specified condition is true.
        //
        // Parameters:
        //   source:
        //     A sequence to return elements from.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains the elements from
        //     the input sequence that occur before the element at which the test no longer
        //     passes.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        public EnumerableRepository<TEntity> TakeWhile(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            return Where(predicate);
        }

        //
        // Summary:
        //     Performs a subsequent ordering of the elements in a sequence in ascending
        //     order according to a key.
        //
        // Parameters:
        //   source:
        //     An System.Linq.IOrderedEnumerable<TElement> that contains elements to sort.
        //
        //   keySelector:
        //     A function to extract a key from each element.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        //   TKey:
        //     The type of the key returned by keySelector.
        //
        // Returns:
        //     An System.Linq.IOrderedEnumerable<TElement> whose elements are sorted according
        //     to a key.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or keySelector is null.
        public EnumerableRepository<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            CheckExecution();
            return OrderBy(keySelector);
        }

        //
        // Summary:
        //     Performs a subsequent ordering of the elements in a sequence in descending
        //     order, according to a key.
        //
        // Parameters:
        //   source:
        //     An System.Linq.IOrderedEnumerable<TElement> that contains elements to sort.
        //
        //   keySelector:
        //     A function to extract a key from each element.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        //   TKey:
        //     The type of the key returned by keySelector.
        //
        // Returns:
        //     An System.Linq.IOrderedEnumerable<TElement> whose elements are sorted in
        //     descending order according to a key.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or keySelector is null.
        public EnumerableRepository<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            CheckExecution();
            return OrderByDescending(keySelector);
        }



        //
        // Summary:
        //     Filters a sequence of values based on a predicate.
        //
        // Parameters:
        //   source:
        //     An System.Collections.Generic.IEnumerable<TEntity> to filter.
        //
        //   predicate:
        //     A function to test each element for a condition.
        //
        // Type parameters:
        //   TEntity:
        //     The type of the elements of source.
        //
        // Returns:
        //     An System.Collections.Generic.IEnumerable<TEntity> that contains elements from
        //     the input sequence that satisfy the condition.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     source or predicate is null.
        public EnumerableRepository<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            this.InternalBuilder.Where($"({WhereImpl(predicate)})", null);
            return this;
        }

        public EnumerableRepository<TEntity> OrWhere(Expression<Func<TEntity, bool>> predicate)
        {
            this.InternalBuilder.OrWhere($"({WhereImpl(predicate)})", null);
            return this;
        }

        string WhereImpl(Expression<Func<TEntity, bool>> predicate)
        {
            CheckExecution();
            List<QueryParameter> queryProperties = new List<QueryParameter>();
            ExpressionParser.ParseExpression(predicate.Body, ExpressionType.Default, ref queryProperties);

            StringBuilder sqlchunk = new StringBuilder();
            foreach (QueryParameter item in queryProperties)
            {
                sqlchunk.Append($"{item.LinkingOperator} ");                
                if (item.PropertyValue != null)
                {
                    if (!string.IsNullOrEmpty(item.PropertyFormat))
                    {
                        sqlchunk.AppendFormat(item.PropertyFormat, InternalBuilder.Parameters.Count);
                        sqlchunk.AppendFormat("{0} {1} ", item.QueryOperator, item.PropertyValue);                    
                    }
                    else
                        sqlchunk.AppendFormat("{0} {1} @p{2} ", item.PropertyName, item.QueryOperator, InternalBuilder.Parameters.Count);
                    InternalBuilder.AddParametersWithCount(item.PropertyValue);
                }
                else
                    sqlchunk.AppendFormat("{0} {1} ", item.PropertyName, item.QueryOperator);
            }

            return sqlchunk.ToString();

        }


    }
}
