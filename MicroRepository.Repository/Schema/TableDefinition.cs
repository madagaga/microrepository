using MicroRepository.Repository;
using MicroRepository.Repository.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Schema
{
    public class TableDefinition
    {
        public string ViewName { get; private set; }
        public string TableName { get; private set; }

        public bool HasIdentity { get; private set; }

        public bool HasView { get { return !string.IsNullOrEmpty(ViewName); } }
        public Dictionary<string, DataBasePropertyAccessor> Members { get; private set; }

        public string SelectTemplate { get; private set; }
        public string CountTemplate { get; private set; }
        public string DeleteTemplate { get; private set; }
        public string InsertTemplate { get; private set; }
        public string UpdateTemplate { get; private set; }


        public TableDefinition(Type targetType, string name = null)
        {
            TypeInfo targetTypeInfo = targetType.GetTypeInfo();
            if (!string.IsNullOrEmpty(name))
                TableName = name;
            else
            {
                System.Attribute attr = targetTypeInfo.GetCustomAttribute<TableAttribute>();
                if (attr != null)
                    TableName = ((TableAttribute)attr).Name;
                else
                    TableName = targetType.Name;

                attr = targetTypeInfo.GetCustomAttribute <ViewAttribute>();
                if (attr != null)
                    ViewName = ((ViewAttribute)attr).Name;
            }

            var cachedProperties = MicroRepository.Core.Caching.ReflectionCache.GetProperties(targetType);


            Members = targetType
                        .GetProperties()
                        .Where(p => p.GetSetMethod() != null && p.GetGetMethod() != null && !p.IsDefined(typeof(NotMappedAttribute)))
                        .Select<PropertyInfo, DataBasePropertyAccessor>(p => new DataBasePropertyAccessor(cachedProperties[p.Name], TableName))
                        .ToDictionary(p => p.Property.Name);


            var template = RepositoryDiscoveryService.Template;

            // identity
            HasIdentity = Members.Values.Any(c => c.IsIdentity);

            // select 
            if (HasView)
                SelectTemplate = string.Format(template.Select, template.Enquote(ViewName) + ".*", template.Enquote(ViewName));
            else
                SelectTemplate = string.Format(template.Select, string.Join(", ", Members.Values.Select(c => c.SelectString)), template.Enquote(TableName));
            
            //count 
            if(HasView)                             
                CountTemplate =  string.Format(template.Select, "COUNT(*)", template.Enquote(ViewName));
            else 
                CountTemplate =  string.Format(template.Select, "COUNT(*)", template.Enquote(TableName));

            // delete 
            DeleteTemplate = string.Format(template.Delete, template.Enquote(TableName));
            
            // update            
            UpdateTemplate =  string.Format(template.Update, template.Enquote(TableName), string.Join(",", Members.Values.Where(c=>!c.IsPrimaryKey).Select(c=>c.UpdateString)));

            // insert
            string tpl = template.Insert;            
            if (Members.Any(c => c.Value.IsIdentity))
                tpl += template.Identity;

            IEnumerable<string> dbCols = Members.Values.Where(c => !c.IsIdentity).Select(c => c.EnquotedDbName);
            IEnumerable<string> objectCols = Members.Values.Where(c => !c.IsIdentity).Select(c => string.Format("@{0}", c.Name));

            InsertTemplate = string.Format(tpl, template.Enquote(TableName), string.Join(", ",dbCols), string.Join(", ", objectCols));            


        }


    }
}
