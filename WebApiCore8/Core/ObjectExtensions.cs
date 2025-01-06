using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class ObjectExtensions
    {
        public static T ToObject<T>(this IDictionary<string, object> source) where T : class, new()
        {
            T val = new T();
            Type type = val.GetType();
            foreach (KeyValuePair<string, object> item in source)
            {
                type.GetProperty(item.Key).SetValue(val, item.Value, null);
            }

            return val;
        }

        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary((PropertyInfo propInfo) => propInfo.Name, (PropertyInfo propInfo) => propInfo.GetValue(source, null));
        }

        public static IDictionary<string, object> AsDictionaryToLower(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary((PropertyInfo propInfo) => propInfo.Name.ToLower(), (PropertyInfo propInfo) => propInfo.GetValue(source, null));
        }
    }
}
