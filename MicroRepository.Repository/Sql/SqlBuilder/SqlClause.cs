using System.Collections.Generic;
using System.Text;

namespace MicroRepository.Sql
{
    public class SqlClauseCollection : List<SqlClause>
    {
        public string KeyWord { get; set; }

        ///<summary>
        /// Returns a string representation of the SqlClauseCollection.
        ///</summary>
        public override string ToString()
        {
            if (this.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.Count; i++)
                if (i == 0)
                    sb.Append(this[i].Sql);
                else
                    sb.Append($"{this[i].Joiner} {this[i].Sql}");

            return sb.ToString();
        }
    }

    public class SqlClause
    {
        public string Sql { get; set; }
        public string Joiner { get; set; }
    }

}
