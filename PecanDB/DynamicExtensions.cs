namespace PecanDB
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public static class DynamicExtensions
    {
        public static dynamic ToDynamic(this object value)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
                expando.Add(property.Name, property.GetValue(value));

            return expando as ExpandoObject;
        }

        public static T FromDynamic<T>(this IDictionary<string, object> dictionary)
        {
            var bindings = new List<MemberBinding>();
            foreach (PropertyInfo sourceProperty in typeof(T).GetProperties().Where(x => x.CanWrite))
            {
                string key = dictionary.Keys.SingleOrDefault(x => x.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(key))
                    continue;
                object propertyValue = dictionary[key];
                bindings.Add(Expression.Bind(sourceProperty, Expression.Constant(propertyValue)));
            }
            Expression memberInit = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            return Expression.Lambda<Func<T>>(memberInit).Compile().Invoke();
        }
    }
}