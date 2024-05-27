using MicroRepository.Repository;
using System.Collections.Generic;
using System.Text;

namespace MicroRepository.Sql
{
    public class SqlBuilder
    {
        static System.Text.RegularExpressions.Regex regex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*[^\*]*\*\*\/", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

        ///<summary>
        /// Initializes a new instance of the SqlBuilder class.
        ///</summary>
        public SqlBuilder() { }

        ///<summary>
        /// Initializes a new instance of the SqlBuilder class with the specified template.
        ///</summary>
        ///<param name="template">The SQL template.</param>
        public SqlBuilder(string template)
        {
            Template = template;
        }

        ///<summary>
        /// Gets or sets the SQL template.
        ///</summary>
        public string Template { get; internal set; }


        Dictionary<string, SqlClauseCollection> _clauses = new Dictionary<string, SqlClauseCollection>();

        ///<summary>
        /// Gets the dynamic parameters for the SQL builder.
        ///</summary>
        public Core.DynamicParameters.DynamicParameter Parameters { get; } = new Core.DynamicParameters.DynamicParameter();

        string _rawSQL;

        ///<summary>
        /// Gets the raw SQL statement.
        ///</summary>
        public string RawSql
        {
            get
            {
                if (string.IsNullOrEmpty(_rawSQL))
                    _rawSQL = this.Compile();
                return _rawSQL;
            }
        }

        ///<summary>
        /// Adds a SQL clause to the builder.
        ///</summary>
        ///<param name="name">The name of the clause.</param>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<param name="joiner">The joiner for multiple clauses.</param>
        ///<param name="keyword">The keyword for the clause.</param>
        public void AddClause(string name, string sql, object parameters, string joiner, string keyword = "")
        {

            SqlClauseCollection clauses = null;
            if (!_clauses.TryGetValue(name, out clauses))
            {
                clauses = new SqlClauseCollection() { KeyWord = keyword };
                _clauses[name] = clauses;
            }

            clauses.Add(new SqlClause() { Sql = sql, Joiner = joiner });

            if (parameters != null)
                AddParameter(parameters);
        }


        ///<summary>
        /// Adds a parameter to the dynamic parameters.
        ///</summary>
        ///<param name="parameter">The parameter to add.</param>
        public void AddParameter(object parameter)
        {
            Parameters.AddDynamicParams(parameter);
        }

        ///<summary>
        /// Adds a parameter to the dynamic parameters with the specified key and value.
        ///</summary>
        ///<param name="key">The key of the parameter.</param>
        ///<param name="value">The value of the parameter.</param>
        public void AddParameter(string key, object value)
        {
            if (Parameters.ContainsKey(key))
                Parameters[key] = value;
            else
                Parameters.Add(key, value);
        }

        ///<summary>
        /// Adds a parameter to the dynamic parameters with an auto-generated key.
        ///</summary>
        ///<param name="value">The value of the parameter.</param>
        public void AddParametersWithCount(object value)
        {
            AddParameter(string.Format("p{0}", Parameters.Count), value);
        }

        #region helpers

        ///<summary>
        /// Adds an INTERSECT clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Intersect(string sql, object parameters = null)
        {
            AddClause("intersect", sql, parameters, joiner: "\nINTERSECT\n ", keyword: "\n ");
            return this;
        }

        ///<summary>
        /// Adds an INNER JOIN clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder InnerJoin(string sql, object parameters = null)
        {
            AddClause("innerjoin", sql, parameters, joiner: "\nINNER JOIN ", keyword: "\nINNER JOIN ");
            return this;
        }

        ///<summary>
        /// Adds a LEFT JOIN clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder LeftJoin(string sql, object parameters = null)
        {
            AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", keyword: "\nLEFT JOIN ");
            return this;
        }

        ///<summary>
        /// Adds a RIGHT JOIN clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder RightJoin(string sql, object parameters = null)
        {
            AddClause("rightjoin", sql, parameters, joiner: "\nRIGHT JOIN ", keyword: "\nRIGHT JOIN ");
            return this;
        }

        ///<summary>
        /// Adds a WHERE clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<param name="index">The index of the WHERE clause.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Where(string sql, object parameters = null, int index = 0)
        {
            if (!string.IsNullOrEmpty(sql))
                AddClause("where", sql, parameters, " AND ", keyword: "WHERE ");
            return this;
        }

        ///<summary>
        /// Adds an OR WHERE clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<param name="index">The index of the WHERE clause.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder OrWhere(string sql, object parameters = null, int index = 0)
        {
            if (!string.IsNullOrEmpty(sql))
                AddClause("where", sql, parameters, " OR ", keyword: "WHERE ");
            return this;
        }

        ///<summary>
        /// Adds an ORDER BY clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder OrderBy(string sql, object parameters = null)
        {
            AddClause("orderby", sql, parameters, " , ", keyword: "ORDER BY ");
            return this;
        }

        ///<summary>
        /// Adds a JOIN clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Join(string sql, object parameters = null)
        {
            AddClause("join", sql, parameters, joiner: "\nJOIN ", keyword: "\nJOIN ");
            return this;
        }

        ///<summary>
        /// Adds a GROUP BY clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder GroupBy(string sql, object parameters = null)
        {
            AddClause("groupby", sql, parameters, joiner: " , ", keyword: "\nGROUP BY ");
            return this;
        }

        ///<summary>
        /// Adds a HAVING clause to the SQL builder.
        ///</summary>
        ///<param name="sql">The SQL statement.</param>
        ///<param name="parameters">The parameters for the SQL statement.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Having(string sql, object parameters = null)
        {
            AddClause("having", sql, parameters, joiner: "\nAND ", keyword: "HAVING ");
            return this;
        }

        ///<summary>
        /// Adds a DISTINCT clause to the SQL builder.
        ///</summary>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Distinct()
        {
            if (!this._clauses.ContainsKey("distinct"))
                AddClause("distinct", "", null, "", keyword: "DISTINCT ");
            return this;
        }

        ///<summary>
        /// Adds a TAKE clause to the SQL builder.
        ///</summary>
        ///<param name="count">The number of rows to take.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Take(int count)
        {
            if (this._clauses.ContainsKey("take"))
                this._clauses.Remove("take");
            string keyword = RepositoryDiscoveryService.Template.Take;


            AddClause("take", count.ToString(), null, "", keyword: keyword);
            return this;

        }

        ///<summary>
        /// Adds a SKIP clause to the SQL builder.
        ///</summary>
        ///<param name="count">The number of rows to skip.</param>
        ///<returns>The updated SQL builder.</returns>
        public SqlBuilder Skip(int count)
        {
            if (this._clauses.ContainsKey("skip"))
                this._clauses.Remove("skip");
            string keyword = RepositoryDiscoveryService.Template.Skip;

            AddClause("skip", count.ToString(), null, "", keyword: keyword);
            return this;

        }

        #endregion

        ///<summary>
        /// Compiles the SQL builder into a raw SQL statement.
        ///</summary>
        ///<returns>The compiled raw SQL statement.</returns>
        public string Compile()
        {
            StringBuilder rawSql = new StringBuilder(Template);

            foreach (KeyValuePair<string, SqlClauseCollection> clause in _clauses)
                rawSql = rawSql.Replace("/**" + clause.Key.ToLower() + "**/", $"{clause.Value.KeyWord} {clause.Value.ToString()}");

            // remove unused clause
            return regex.Replace(rawSql.ToString(), "");
        }
    }
}
