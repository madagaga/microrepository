using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroRepository.HttpQueryFilter
{
    public class QueryParameter
    {
        static string[] operators = new string[] { "<>", "<=", ">=", ">", "<", "=", "%" };

        public List<QueryFilter> Filters { get; private set; }
        public List<QuerySorter> Sorters { get; private set; }

        public string[] Columns { get; private set; }

        internal int _rows;
        public int Rows { get { return _rows; } }
        int _skip = 0;
        public int Skip { get { return _skip; } }

        internal QueryParameter()
        {
            Filters = new List<QueryFilter>();
            Sorters = new List<QuerySorter>();
        }

        public static QueryParameter Parse( string queryString)
        {

           
            QueryParameter qp = new QueryParameter();
            if (!string.IsNullOrEmpty(queryString))
            {
                string[] pairs = queryString.Trim('?').Split('&'), kvp;

                foreach (string pair in pairs)
                {
                    kvp = pair.Split('=');
                    switch (kvp[0])
                    {
                        case "sort":
                            if (kvp[1].StartsWith("-"))
                                qp.Sorters.Add(new QuerySorter() { Name = kvp[1].Substring(1), Order = "DESC" });
                            else
                                qp.Sorters.Add(new QuerySorter() { Name = kvp[1], Order = "ASC" });
                            break;
                        case "take":
                            int.TryParse(kvp[1], out qp._rows);
                            break;
                        case "skip":
                            int.TryParse(kvp[1], out qp._skip);
                            break;
                        case "filter":
                            qp.ParseParameter(Uri.UnescapeDataString(kvp[1]));
                            break;
                        case "select":
                            qp.Columns = Uri.UnescapeDataString(kvp[1]).Split(',');
                            break;
                        default:
                            break;

                    }
                }
            }
            
            return qp;


        }

        public QueryParameter ParseParameter(string queryString)
        {
            string[] pairs = queryString.Split('&'), kvp;
            string op;
            foreach (string pair in pairs)
            {
                op = detectOperator(pair);
                kvp = pair.Split(op.ToArray());
                this.Filters.Add(new QueryFilter() { ColumnName = kvp[0], Operator = op, Value = kvp[1] });
            }
            return this;
        }        

        static string detectOperator(string queryStringPair)
        {
            foreach (string op in operators)
                if (queryStringPair.IndexOf(op) > -1)
                    return op;

            throw new Exception("Operator not found");
        }

       
    }
}
