using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MicroRepository.Core.Schema
{

    // inspired by microsoft compiled property accessor
    public class CompiledPropertyAccessor<T>
    {        
        private Action<T, object> _setter;
        private Func<T, object> _getter;
        
        public PropertyInfo Property
        {
            get;
            private set;
        }

        public Type Type
        {
            get;
            private set;
        }


        public CompiledPropertyAccessor(PropertyInfo property)            
        {
            Property = property;
            Type = property.PropertyType;
            _setter = MakeSetter(property);
            _getter = MakeGetter( property);
        }

        public object Get(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
            return _getter(entity);
        }

        public void Set(T entity, object value)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            try
            {
                _setter(entity, value);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to set value '{value}' on property {Property.Name}", e);
            }
        }

        private static Action<T, object> MakeSetter(PropertyInfo property)
        {            
            ParameterExpression entityParameter = Expression.Parameter(typeof(T));
            ParameterExpression objectParameter = Expression.Parameter(typeof(Object));
            MemberExpression toProperty = Expression.Property(Expression.TypeAs(entityParameter, property.DeclaringType), property);            
            UnaryExpression fromValue = Expression.Convert(objectParameter, property.PropertyType);
            BinaryExpression assignment = Expression.Assign(toProperty, fromValue);
            Expression<Action<T, object>> lambda = Expression.Lambda<Action<T, object>>(assignment, entityParameter, objectParameter);
            return lambda.Compile();
        }

        private static Func<T, object> MakeGetter(PropertyInfo property)
        {
            ParameterExpression entityParameter = Expression.Parameter(typeof(T));
            MemberExpression fromProperty = Expression.Property(Expression.TypeAs(entityParameter, property.DeclaringType), property);
            UnaryExpression convert = Expression.Convert(fromProperty, typeof(Object));
            Expression<Func<T, object>> lambda = Expression.Lambda<Func<T, object>>(convert, entityParameter);
            return lambda.Compile();
        }

        public void Copy(T from, T to)
        {
            if (from == null)
            {
                throw new ArgumentNullException("from");
            }
            if (to == null)
            {
                throw new ArgumentNullException("to");
            }
            Set(to, Get(from));
        }                
    }
}
