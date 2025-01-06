using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core
{
    public static class Utils
    {
        public static bool IsNumber(string s)
        {
            bool flag = true;
            char[] array = s.ToCharArray();
            foreach (char c in array)
            {
                flag = flag && char.IsDigit(c);
            }

            return flag;
        }

        public static string ConvertDataTableToJson(DataTable dataTable)
        {
            try
            {
                return JsonConvert.SerializeObject(dataTable);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string ConvertToJson<T>(T value)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return value?.ToString();
                }

                return JsonConvert.SerializeObject(value);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static T JsonToObject<T>(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return default(T);
                }

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static string GenerateString(int length = 20)
        {
            string text = "ABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            char[] array = new char[length];
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                array[i] = text[random.Next(0, text.Length)];
            }

            return new string(array);
        }

        public static string UserKey(string username, string expireTime)
        {
            return username + ":" + expireTime;
        }

       

        public static DateTime LongToDateTime(long value)
        {
            long ticks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            return new DateTime(ticks + value * 10000, DateTimeKind.Utc);
        }

        public static DateTime IntToDateTime(int value)
        {
            return DateTimeOffset.UtcNow.AddSeconds(value).UtcDateTime;
        }

        public static object ToLowerObject<T>(T obj)
        {
            IDictionary<string, object> source = obj.AsDictionaryToLower();
            return source.ToObject<object>();
        }

        public static string GetStringParam(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "null";
            }

            value = value.Replace("'", "''");
            return "'" + value + "'";
        }

        public static string AddCommasLast(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new Exception("query is not null");
            }

            if (!query.Substring(query.Length - 1, 1).Equals(";"))
            {
                query += ";";
            }

            return query;
        }

        public static string SearchLike(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return ("%" + value + "%").ToLower();
        }

        public static string StringNullOrEmpty(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return value;
        }

        public static string StringNullReturnEmpty(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value;
        }

        public static string SnakeCaseToCamelCaseJson(string json)
        {
            List<Dictionary<string, object>> list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            List<Dictionary<string, object>> list2 = new List<Dictionary<string, object>>();
            foreach (Dictionary<string, object> item in list)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                foreach (string key in item.Keys)
                {
                    object value = item[key];
                    dictionary.Add(ToCamelCaseStr(key), value);
                }

                list2.Add(dictionary);
            }

            return ConvertToJson(list2);
        }

        public static string ToCamelCaseStr(string str)
        {
            string[] array = str.Split(new string[2] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);
            string text = array[0].ToLower();
            string[] value = (from word in array.Skip(1)
                              select char.ToUpper(word[0]) + word.Substring(1)).ToArray();
            return text + string.Join(string.Empty, value);
        }

        public static bool Valid<T>(List<T> data)
        {
            if (data != null && data.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static int ConvertStringToInt(string value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (Exception)
            {
                return 0;
            }
        }


        private static T GetItem<T>(DataRow dr)
        {
            Type typeFromHandle = typeof(T);
            T val = Activator.CreateInstance<T>();
            foreach (DataColumn column in dr.Table.Columns)
            {
                PropertyInfo[] properties = typeFromHandle.GetProperties();
                foreach (PropertyInfo propertyInfo in properties)
                {
                    try
                    {
                        if (!string.Equals(propertyInfo.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        object obj = dr[column.ColumnName];
                        bool flag = obj != null;
                        bool flag2 = obj?.ToString() != "";
                        if ((!(propertyInfo.PropertyType == typeof(int?)) && !(propertyInfo.PropertyType == typeof(long?)) && !(propertyInfo.PropertyType == typeof(float?)) && !(propertyInfo.PropertyType == typeof(double?)) && !(propertyInfo.PropertyType == typeof(decimal?)) && !(propertyInfo.PropertyType == typeof(DateTime?))) || flag2)
                        {
                            if (propertyInfo.PropertyType == typeof(DateTime?) && flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, typeof(DateTime)), null);
                            }
                            else if (propertyInfo.PropertyType == typeof(int?) && flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, typeof(int)), null);
                            }
                            else if (propertyInfo.PropertyType == typeof(long?) && flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, typeof(long)), null);
                            }
                            else if (propertyInfo.PropertyType == typeof(float?) && flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, typeof(float)), null);
                            }
                            else if (propertyInfo.PropertyType == typeof(double?) && flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, typeof(double)), null);
                            }
                            else if (propertyInfo.PropertyType == typeof(decimal?) && flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, typeof(decimal)), null);
                            }
                            else if (flag && propertyInfo.PropertyType == typeof(string))
                            {
                                string value = obj.ToString().Trim();
                                propertyInfo.SetValue(val, value, null);
                            }
                            else if (flag2)
                            {
                                propertyInfo.SetValue(val, Convert.ChangeType(obj, propertyInfo.PropertyType), null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error Column: " + column.ColumnName + " || " + ex.ToString());
                    }
                }
            }

            return val;
        }

        public static List<T> ConvertDataTableToList<T>(DataTable dt)
        {
            List<T> list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                list.Add(item);
            }

            return list;
        }
    }
}
