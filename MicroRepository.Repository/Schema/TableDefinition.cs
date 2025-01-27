using MicroRepository.Core.Caching;
using MicroRepository.Repository.Attributes;
using MicroRepository.Repository;
using MicroRepository.Templates;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace MicroRepository.Schema
{
    /// <summary>
    /// Defines table metadata for an entity type.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TableDefinition"/> class.
        /// </summary>
        /// <param name="targetType">The type representing the entity.</param>
        /// <param name="name">Optional custom table name.</param>
        public TableDefinition(Type targetType, string? name = null)
        {
            ArgumentNullException.ThrowIfNull(targetType, nameof(targetType));

            var template = RepositoryDiscoveryService.Template;
            if (template == null)
            {
                throw new InvalidOperationException("Repository template is not initialized.");
            }

            InitializeTableName(targetType, name, template);
            InitializeMembers(targetType, template);
            InitializeTemplates(template);
        }

        /// <summary>
        /// Initializes the table name and view-related metadata.
        /// </summary>
        private void InitializeTableName(Type targetType, string? name, SqlTemplate template)
        {
            var targetTypeInfo = targetType.GetTypeInfo();
            var viewName = string.Empty;

            // Handle custom table name first
            if (!string.IsNullOrEmpty(name))
            {
                TableName = name;
            }
            else
            {
                // Check for [Table] attribute
                var tableAttribute = targetTypeInfo.GetCustomAttribute<TableAttribute>();
                TableName = tableAttribute?.Name ?? targetType.Name;

                // Check for [View] attribute
                var viewAttribute = targetTypeInfo.GetCustomAttribute<ViewAttribute>();
                if (viewAttribute != null)
                {
                    viewName = viewAttribute.Name;
                    SelectTableName = $"{template.Enquote(viewName)} AS {template.Enquote(TableName)}";
                }
                else
                {
                    SelectTableName = template.Enquote(TableName);
                }
            }
        }

        /// <summary>
        /// Initializes the properties (members) of the entity, including metadata for mapping.
        /// </summary>
        private void InitializeMembers(Type targetType, SqlTemplate template)
        {
            var cachedProperties = ReflectionCache.GetProperties(targetType);

            Members = cachedProperties.Values
                .Where(prop => !prop.Property.IsDefined(typeof(NotMappedAttribute)))
                .Select(prop => new DataBasePropertyAccessor(prop, TableName))
                .ToDictionary(accessor => accessor.Name);

            HasIdentity = Members.Values.Any(member => member.IsIdentity);
        }

        /// <summary>
        /// Initializes SQL templates for various CRUD operations.
        /// </summary>
        private void InitializeTemplates(SqlTemplate template)
        {
            // SELECT template
            SelectTemplate = Members.Values.Any(member => member.IsIdentity)
                ? string.Format(template.Select, template.Enquote(TableName) + ".*", SelectTableName)
                : string.Format(template.Select, string.Join(", ", Members.Values.Select(member => member.SelectString)), SelectTableName);

            // COUNT template
            CountTemplate = string.Format(template.Select, "COUNT(*)", SelectTableName);

            // DELETE template
            DeleteTemplate = string.Format(template.Delete, template.Enquote(TableName));

            // UPDATE template
            UpdateTemplate = string.Format(template.Update,
                template.Enquote(TableName),
                string.Join(", ", Members.Values.Where(member => !member.IsPrimaryKey).Select(member => member.UpdateString)));

            // INSERT template
            var insertTemplate = template.Insert;
            if (HasIdentity)
            {
                insertTemplate += template.Identity; // Append identity retrieval (e.g., SCOPE_IDENTITY)
            }

            var dbColumns = Members.Values
                .Where(member => !member.IsIdentity)
                .Select(member => member.EnquotedDbName);

            var objectColumns = Members.Values
                .Where(member => !member.IsIdentity)
                .Select(member => $"@{member.Name}");

            InsertTemplate = string.Format(insertTemplate,
                template.Enquote(TableName),
                string.Join(", ", dbColumns),
                string.Join(", ", objectColumns));
        }
    }
}