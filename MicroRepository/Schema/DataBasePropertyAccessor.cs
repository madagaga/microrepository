using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using MicroRepository.Caching;
using MicroRepository.Repository;

namespace MicroRepository.Schema
{
    public class DataBasePropertyAccessor : CompiledPropertyAccessor<object>
    {
        public string TableName { get; set; }
        public string Name { get { return Property.Name; } }
        public Type Type { get { return Property.PropertyType; } }
        
        public string EnquotedDbName { get; private set; }
        public string EnquotedTableName { get; private set; }
        public bool IsPrimaryKey { get; private set; }
        public bool IsIdentity { get; private set; }


        string _dBName;

        string _selectString;
        internal string SelectString
        {
            get
            {
                if (string.IsNullOrEmpty(_selectString))
                {
                    if (Name != _dBName)
                        if (DbFactory.DataBaseType == Enums.DatabaseType.SQLite)
                            _selectString = string.Format("{0} AS {1}", EnquotedDbName, DbFactory.Template.Enquote(Name));
                        else
                            _selectString = string.Format("{0}.{1} AS {0}.{2}", EnquotedTableName, EnquotedDbName, DbFactory.Template.Enquote(Name));
                    else
                        _selectString = string.Format("{0}.{1}", EnquotedTableName, EnquotedDbName);
                }
                return _selectString;
            }
        }

        string _updateString;
        internal string UpdateString
        {
            get
            {
                if (string.IsNullOrEmpty(_updateString))
                    if (DbFactory.DataBaseType == Enums.DatabaseType.SQLite)
                        _updateString = string.Format("{0} = @{1}", EnquotedDbName, Name);
                    else
                        _updateString = string.Format("{0}.{1} = @{2}", EnquotedTableName, EnquotedDbName, Name);
                
                return _updateString;
            }
        }


        //public bool IsRelation { get; private set; }

        public DataBasePropertyAccessor(System.Reflection.PropertyInfo keyProperty, string tableName)
            : base(keyProperty)
        {
            TableName = tableName;
            var name = keyProperty.GetCustomAttribute<ColumnAttribute>();
            this._dBName = name == null ? keyProperty.Name : name.Name;

            EnquotedDbName = DbFactory.Template.Enquote(_dBName);
            EnquotedTableName = DbFactory.Template.Enquote(TableName);
            IsPrimaryKey = keyProperty.IsDefined(typeof(KeyAttribute));
            var isIdentity = keyProperty.GetCustomAttribute<DatabaseGeneratedAttribute>();
            this.IsIdentity = isIdentity != null && isIdentity.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;
            //var isRelation = keyProperty.GetCustomAttribute<RelationAttribute>();
            //this.IsRelation = isRelation != null;
        }

        
                
    }
}
