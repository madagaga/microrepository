using MicroRepository.Core.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroRepository.Core.DynamicParameters
{
    /// <summary>
    /// Represents a dynamic parameter dictionary.
    /// </summary>
    public class DynamicParameter : Dictionary<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicParameter"/> class.
        /// </summary>
        public DynamicParameter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicParameter"/> class with the specified object.
        /// </summary>
        /// <param name="o">The object to add as dynamic parameters.</param>
        public DynamicParameter(object o)
        {
            if (o == null)
                return;
            if (o is DynamicParameter)
                this.Merge(o as Dictionary<string, object>);
            else
            {
                this.AddDynamicParams(o);
            }
        }

        /// <summary>
        /// Adds dynamic parameters from an object.
        /// </summary>
        /// <param name="o">The object to add as dynamic parameters.</param>
        public void AddDynamicParams(object o)
        {
            if (o == null)
                return;

            if (o is DynamicParameter || o is IDictionary)
                this.Merge(o as Dictionary<string, object>);
            else if (o is IList)
                throw new Exception("List is not handled");
            else
            {
                var properties = ReflectionCache.GetProperties(o.GetType());
                object instance;

                foreach (var kvp in properties)
                {
                    instance = kvp.Value.Get(o);

                    // if list or dictionary then 
                    if (instance != null && kvp.Value.Property.PropertyType is IDictionary)
                    {
                        // iterate
                        foreach (var element in instance as Dictionary<string, object>)
                            this.Add(element.Key, element.Value);
                    }
                    else
                        this.Add(kvp.Key, instance);

                }
            }
        }

        /// <summary>
        /// Merges the given dynamic parameters into the current instance.
        /// </summary>
        /// <param name="dynamicParameter">The dynamic parameters to merge.</param>
        public void Merge(Dictionary<string, object> dynamicParameter)
        {
            foreach (KeyValuePair<string, object> kvp in dynamicParameter)
                this[kvp.Key] = kvp.Value;

        }
    }
}
