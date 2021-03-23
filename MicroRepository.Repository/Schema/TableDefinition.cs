using MicroRepository.Repository;
using MicroRepository.Repository.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace MicroRepository.Schema
{
    public class TableDefinition
    {        
        public string TableName { get; private set; }
        public string SelectTableName { get; private set; }

        public bool HasIdentity { get; private set; }
                
        public Dictionary<string, DataBasePropertyAccessor> Members { get; private set; }

        public string SelectTemplate { get; private set; }
        public string CountTemplate { get; private set; }
        public string DeleteTemplate { get; private set; }
        public string InsertTemplate { get; private set; }
        public string UpdateTemplate { get; private set; }


        public TableDefinition(Type targetType, string name = null)
        {
            TypeInfo targetTypeInfo = targetType.GetTypeInfo();
            string viewName = string.Empty;
            var template = RepositoryDiscoveryService.Template;

            if (!string.IsNullOrEmpty(name))
                TableName = name;
            else
            {
                System.Attribute attr = targetTypeInfo.GetCustomAttribute<TableAttribute>();
                if (attr != null)
                    TableName = ((TableAttribute)attr).Name;
                else
                    TableName = targetType.Name;

                attr = targetTypeInfo.GetCustomAttribute<ViewAttribute>();
                if (attr != null)
                {
                    viewName = ((ViewAttribute)attr).Name;
                    SelectTableName = $"{template.Enquote(viewName)} AS {template.Enquote(TableName)}";
                }
                else
                    SelectTableName = template.Enquote(TableName);
                
            }

            var cachedProperties = MicroRepository.Core.Caching.ReflectionCache.GetProperties(targetType);

            Members = cachedProperties.Values.
                Where(c => !c.Property.IsDefined(typeof(NotMappedAttribute)))
                .Select(p => new DataBasePropertyAccessor(p, TableName))
                .ToDictionary(p => p.Name);


            

            // identity
            HasIdentity = Members.Values.Any(c => c.IsIdentity);

            
            // select 
            if (!string.IsNullOrEmpty(viewName))
                SelectTemplate = string.Format(template.Select, template.Enquote(TableName) + ".*", SelectTableName);
            else
                SelectTemplate = string.Format(template.Select, string.Join(", ", Members.Values.Select(c => c.SelectString)), SelectTableName);

            //count 
                CountTemplate = string.Format(template.Select, "COUNT(*)", SelectTableName);

            // delete 
            DeleteTemplate = string.Format(template.Delete, template.Enquote(TableName));

            // update            
            UpdateTemplate = string.Format(template.Update, template.Enquote(TableName), string.Join(",", Members.Values.Where(c => !c.IsPrimaryKey).Select(c => c.UpdateString)));

            // insert
            string tpl = template.Insert;
            if (Members.Any(c => c.Value.IsIdentity))
                tpl += template.Identity;

            IEnumerable<string> dbCols = Members.Values.Where(c => !c.IsIdentity).Select(c => c.EnquotedDbName);
            IEnumerable<string> objectCols = Members.Values.Where(c => !c.IsIdentity).Select(c => string.Format("@{0}", c.Name));

            InsertTemplate = string.Format(tpl, template.Enquote(TableName), string.Join(", ", dbCols), string.Join(", ", objectCols));


        }


    }
}
