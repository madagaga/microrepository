using System;
using System.Collections.Generic;
using System.Text;

namespace MicroRepository.Sql
{
    public class SqlClauseCollection : List<SqlClause>
    {
        public string KeyWord { get; set; }
    }

    public class SqlClause
    {   
        public string Sql { get; set; }        
        public string Joiner { get; set; }                
        
    }
        
}
