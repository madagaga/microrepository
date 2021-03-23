using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MicroRepository.Schema
{
    public class Delta<T> where T : class
    {

        private readonly Dictionary<string, DataBasePropertyAccessor> _propertiesThatExist;
        private HashSet<string> _changedProperties;
        private readonly T _entity;
        private readonly Type _entityType;

        private readonly HashSet<string> _ignoredProperties;

        /// <summary>
        /// Initializes a new instance of <see cref="Delta{T}"/>.
        /// </summary>
        public Delta(T changed)
        {
            _entity = changed;
            _entityType = typeof(T);
            _propertiesThatExist = Caching.TableDefinitionCache.GetPropertiesDictionary(typeof(T));
            _ignoredProperties = new HashSet<string>();

        }

        public Delta<T> Exclude(string propertyName)
        {
            if (_propertiesThatExist.ContainsKey(propertyName))
                this._ignoredProperties.Add(propertyName);
            else
                throw new InvalidOperationException("Property '" + propertyName + "' is not a member of '" + _entityType.Name + "'");
            return this;
        }


        /// <summary>
        /// Overwrites the <paramref name="original"/> entity with the changes tracked by this Delta.
        /// <remarks>The semantics of this operation are equivalent to a HTTP PATCH operation, hence the name.</remarks>
        /// </summary>
        /// <param name="original">The entity to be updated.</param>
        public void Patch(T original)
        {
            if (original == null)
            {
                throw new ArgumentNullException("original");
            }

            if (!_entityType.IsAssignableFrom(original.GetType()))
            {
                throw new ArgumentException("Delta type mismatch", "original");
            }

            Compare(original);
            DataBasePropertyAccessor[] propertiesToCopy = GetChangedPropertyNames().Select(s => _propertiesThatExist[s]).ToArray();
            foreach (DataBasePropertyAccessor propertyToCopy in propertiesToCopy)
            {
                propertyToCopy.Copy(_entity, original);
            }
        }

        /// <summary>
        /// Overwrites the <paramref name="original"/> entity with the values stored in this Delta.
        /// <remarks>The semantics of this operation are equivalent to a HTTP PUT operation, hence the name.</remarks>
        /// </summary>
        /// <param name="original">The entity to be updated.</param>
        public void Put(T original)
        {
            Patch(original);

            DataBasePropertyAccessor[] propertiesToCopy = GetUnchangedPropertyNames().Select(s => _propertiesThatExist[s]).ToArray();
            foreach (DataBasePropertyAccessor propertyToCopy in propertiesToCopy)
            {
                propertyToCopy.Copy(_entity, original);
            }
        }


        /// <summary>
        /// Returns the Properties that have been modified through this Delta as an 
        /// enumeration of Property Names 
        /// </summary>
        public IEnumerable<string> GetChangedPropertyNames()
        {
            return _changedProperties;
        }

        public void Compare(T original, bool excludeNull = true)
        {
            _changedProperties = new HashSet<string>();
            object v1, v2;
            foreach (var property in _propertiesThatExist.Where(c => !_ignoredProperties.Contains(c.Key)))
            {
                v1 = property.Value.Get(_entity);
                v2 = property.Value.Get(original);
                if (v1 == null)
                {
                    if (excludeNull)
                        continue;
                    else if (v2 != null)
                        _changedProperties.Add(property.Key);
                }
                else if (!v1.Equals(v2))
                    _changedProperties.Add(property.Key);

            }
        }

        /// <summary>
        /// Returns the Properties that have not been modified through this Delta as an 
        /// enumeration of Property Names 
        /// </summary>
        public IEnumerable<string> GetUnchangedPropertyNames()
        {
            return _propertiesThatExist.Keys.Except(GetChangedPropertyNames());
        }

        public DataBasePropertyAccessor[] GetChangedProperties()
        {
            return _changedProperties.Select(c => _propertiesThatExist[c]).ToArray();
        }



    }

    public static class DeltaExtension
    {
        public static Delta<T> Exclude<T, TKey>(this Delta<T> delta, Expression<Func<T, TKey>> selector) where T : class
        {
            MemberExpression body = (MemberExpression)selector.Body;
            return delta.Exclude(body.Member.Name);
        }
    }
}
