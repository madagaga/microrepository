using MicroRepository.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MicroRepository.DynamicParameters
{
    public class DynamicParameter:Dictionary<string, object>
    {
        public DynamicParameter() { }

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

        public void Merge(Dictionary<string, object> dynamicParameter)
        {
            foreach (KeyValuePair<string, object> kvp in dynamicParameter)
                if (this.ContainsKey(kvp.Key))
                    this[kvp.Key] = kvp.Value;
                else
                    this.Add(kvp.Key, kvp.Value);
        }
    }
}
