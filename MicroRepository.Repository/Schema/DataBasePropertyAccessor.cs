using MicroRepository.Core.Schema;
using MicroRepository.Repository;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace MicroRepository.Schema
{
    public class DataBasePropertyAccessor
    {
        public string Name { get { return Property.Name; } }
        public Type Type { get { return Property.PropertyType; } }

        public PropertyInfo Property { get { return _compiledPropertyAccessor.Property; } }

        public string EnquotedDbName { get; private set; }
        public string EnquotedFullName { get; private set; }
        public bool IsPrimaryKey { get; private set; }
        public bool IsIdentity { get; private set; }

        readonly string _dBName;

        internal CompiledPropertyAccessor<object> _compiledPropertyAccessor;


        string _selectString;
        internal string SelectString
        {
            get
            {
                if (string.IsNullOrEmpty(_selectString))
                {
                    if (Name != _dBName)
                        _selectString = $"{EnquotedFullName} AS {RepositoryDiscoveryService.Template.Enquote(Name)}";
                    else
                        _selectString = $"{EnquotedFullName}";
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
                    _updateString = $"{EnquotedDbName} = @{Name}";

                return _updateString;
            }
        }

        internal void Copy<T>(T entity, T original) where T : class
        {
            _compiledPropertyAccessor.Copy(entity, original);
        }


        //public bool IsRelation { get; private set; }

        public DataBasePropertyAccessor(CompiledPropertyAccessor<object> compiledProperty, string tableName)
        {
            _compiledPropertyAccessor = compiledProperty;


            var name = _compiledPropertyAccessor.Property.GetCustomAttribute<ColumnAttribute>();
            this._dBName = name == null ? _compiledPropertyAccessor.Property.Name : name.Name;

            EnquotedDbName = RepositoryDiscoveryService.Template.Enquote(_dBName);
            EnquotedFullName = $"{RepositoryDiscoveryService.Template.Enquote(tableName)}.{EnquotedDbName}";
            IsPrimaryKey = _compiledPropertyAccessor.Property.IsDefined(typeof(KeyAttribute));
            var isIdentity = _compiledPropertyAccessor.Property.GetCustomAttribute<DatabaseGeneratedAttribute>();
            this.IsIdentity = isIdentity != null && isIdentity.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;
            //var isRelation = keyProperty.GetCustomAttribute<RelationAttribute>();
            //this.IsRelation = isRelation != null;
        }

        internal object Get<T>(T entity) where T : class
        {
            return _compiledPropertyAccessor.Get(entity);
        }
    }
}
