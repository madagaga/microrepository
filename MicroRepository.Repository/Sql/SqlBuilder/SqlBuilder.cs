using MicroRepository.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroRepository.Sql
{
    public class SqlBuilder
    {
        static System.Text.RegularExpressions.Regex regex =
                new System.Text.RegularExpressions.Regex(@"\/\*\*[^\*]*\*\*\/", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline);

        public SqlBuilder() { }
        public SqlBuilder(string template)
        {
            Template = template;
        }

        public string Template { get; set; }


        Dictionary<string, SqlClauseCollection> _clauses = new Dictionary<string, SqlClauseCollection>();
                
        public Core.DynamicParameters.DynamicParameter Parameters { get; } = new Core.DynamicParameters.DynamicParameter();

        public string _rawSQL;
        public string RawSql
        {
            get
            {
                if (string.IsNullOrEmpty(_rawSQL))
                    _rawSQL = this.Compile();
                return _rawSQL;
            }
        }
                
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
                

        public void AddParameter(object parameter)
        {
            Parameters.AddDynamicParams(parameter);
        }

        public void AddParameter(string key, object value)
        {
            if (Parameters.ContainsKey(key))
                Parameters[key] = value;
            else 
                Parameters.Add(key, value);
        }

        public void AddParametersWithCount(object value)
        {
            AddParameter(string.Format("p{0}", Parameters.Count), value);
        }

        #region helpers

        public SqlBuilder Intersect(string sql, object parameters = null)
        {
            AddClause("intersect", sql, parameters, joiner: "\nINTERSECT\n ", keyword: "\n ");
            return this;
        }

        public SqlBuilder InnerJoin(string sql, object parameters = null)
        {
            AddClause("innerjoin", sql, parameters, joiner: "\nINNER JOIN ", keyword: "\nINNER JOIN ");
            return this;
        }

        public SqlBuilder LeftJoin(string sql, object parameters = null)
        {
            AddClause("leftjoin", sql, parameters, joiner: "\nLEFT JOIN ", keyword: "\nLEFT JOIN ");
            return this;
        }

        public SqlBuilder RightJoin(string sql, object parameters = null)
        {
            AddClause("rightjoin", sql, parameters, joiner: "\nRIGHT JOIN ", keyword: "\nRIGHT JOIN ");
            return this;
        }

        public SqlBuilder Where(string sql, object parameters = null, int index = 0)
        {
            if (!string.IsNullOrEmpty(sql))
                AddClause("where", sql, parameters, " AND ", keyword: "WHERE ");//, predicateIndex: index);
            return this;
        }

        public SqlBuilder OrWhere(string sql, object parameters = null, int index = 0)
        {
            if (!string.IsNullOrEmpty(sql))
                AddClause("where", sql, parameters, " OR ", keyword: "WHERE ");//, predicateIndex: index);
            return this;
        }

        public SqlBuilder OrderBy(string sql, object parameters = null)
        {
            AddClause("orderby", sql, parameters, " , ", keyword: "ORDER BY ");
            return this;
        }

        public SqlBuilder Join(string sql, object parameters = null)
        {
            AddClause("join", sql, parameters, joiner: "\nJOIN ", keyword: "\nJOIN ");
            return this;
        }

        public SqlBuilder GroupBy(string sql, object parameters = null)
        {
            AddClause("groupby", sql, parameters, joiner: " , ", keyword: "\nGROUP BY ");
            return this;
        }

        public SqlBuilder Having(string sql, object parameters = null)
        {
            AddClause("having", sql, parameters, joiner: "\nAND ", keyword: "HAVING ");
            return this;
        }

        public SqlBuilder Distinct()
        {   
            if (!this._clauses.ContainsKey("distinct"))
                AddClause("distinct", "", null, "", keyword: "DISTINCT ");
            return this;
        }



        public SqlBuilder Take(int count)
        {
            if (this._clauses.ContainsKey("take"))
                this._clauses.Remove("take");
            string keyword = RepositoryDiscoveryService.Template.Take;

            //switch (RepositoryDiscoveryService.DataBaseType)
            //{
            //    case Enums.DatabaseType.Auto:
            //        break;
            //    case Enums.DatabaseType.MSSql:
            //        keyword = "TOP ";
            //        break;
            //    case Enums.DatabaseType.MySql:
            //    case Enums.DatabaseType.SQLite:                    
            //        keyword = "LIMIT ";
            //        break;
            //    default:
            //        break;
            //}

            AddClause("take", count.ToString(), null, "", keyword: keyword);
            return this;

        }
        public SqlBuilder Skip(int count)
        {
            if (this._clauses.ContainsKey("skip"))
                this._clauses.Remove("skip");
            string keyword = RepositoryDiscoveryService.Template.Skip;

            //switch (RepositoryDiscoveryService.DataBaseType)
            //{
            //    case Enums.DatabaseType.Auto:
            //        break;
            //    case Enums.DatabaseType.MSSql:
            //        keyword = "TOP ";
            //        break;
            //    case Enums.DatabaseType.MySql:
            //    case Enums.DatabaseType.SQLite:                    
            //        keyword = "LIMIT ";
            //        break;
            //    default:
            //        break;
            //}

            AddClause("skip", count.ToString(), null, "", keyword: keyword);
            return this;

        }




        #endregion


        public string Compile()
        {
            StringBuilder rawSql = new StringBuilder(Template);
                        

            SqlClause firstClause = null;
            string clauseString = null;
            foreach (KeyValuePair<string, SqlClauseCollection> clause in _clauses)
            {
                firstClause = clause.Value.First();
                clauseString = string.Join("",clause.Value.Select(c => string.Format("{0} {1}", c.Joiner, c.Sql))).TrimStart(firstClause.Joiner.ToArray());
                rawSql = rawSql.Replace("/**" + clause.Key.ToLower() + "**/", clause.Value.KeyWord + " " + clauseString);
            }
            return regex.Replace(rawSql.ToString(), "");
        }
    }
}
