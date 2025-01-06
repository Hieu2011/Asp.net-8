using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core
{
    public static class Helper
    {
        public static string REGEX_PATTERN_CHECK_EMAIL = "^([\\w\\.\\-]+)@([\\w\\-]+)((\\.(\\w){2,3})+)$";

        public static string REGEX_PATTERN_CHECK_PHONE_NUMBER = "^\\(?([0-9]{3})\\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$";
        public static string ObjectToJson<T>(T model)
        {
            if (model == null)
            {
                return "";
            }

            return JsonConvert.SerializeObject(model);
        }

        public static T JsonToObject<T>(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return default(T);
                }

                //return JsonConvert.DeserializeObject<T>(value, new JsonSerializerSettings()
                //{
                //    MissingMemberHandling = MissingMemberHandling.Ignore,
                //    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                //    NullValueHandling = NullValueHandling.Ignore,
                //});

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public static Dictionary<string, object> JsonToDictionary(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new Dictionary<string, object>();
            }

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
        }

        public static double ConvertStringToDouble(string number)
        {
            try
            {
                return double.Parse(number);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string GetStringFromEnum<T>(T folderChildren)
        {
            try
            {
                if (folderChildren != null)
                {
                    return System.Enum.GetName(typeof(T), folderChildren).ToString().ToLower();
                }
            }
            catch (Exception)
            {
            }
            return "";
        }

        public static bool CheckValidGuid(string text)
        {
            try
            {
                Guid.Parse(text);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static List<string> GetEnumDescriptions<T>()
        {
            var descs = new List<string>();
            var names = Enum.GetNames(typeof(T));
            foreach (var name in names)
            {
                var field = typeof(T).GetField(name);
                var fds = field.GetCustomAttributes(typeof(DescriptionAttribute), true);
                foreach (DescriptionAttribute fd in fds)
                {
                    descs.Add(fd.Description);
                }
            }
            return descs;
        }

        public static string GetDescription<T>(T value)
        {
            var field = (DescriptionAttribute)(typeof(T).GetField(value.ToString()).GetCustomAttribute(typeof(DescriptionAttribute), true));
            if (field != null)
            {
                return field.Description;
            }

            return "";
        }

        public static List<object> GetEnumValue<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Select(v => (object)v).ToList();
        }

        public static string[] GetEnumNames<T>()
        {
            return System.Enum.GetNames(typeof(T));
        }

        public static List<EnumDescription> GetDescriptions<T>()
        {
            List<EnumDescription> result = new List<EnumDescription>();
            List<string> descriptions = GetEnumDescriptions<T>();
            List<object> values = GetEnumValue<T>();

            for (int i = 0; i < descriptions.Count; i++)
            {
                result.Add(new EnumDescription() { Id = (int)values[i], Value = descriptions[i] });
            }
            return result;
        }

        public static List<int> ConvertStringToInts(string ids)
        {
            if (string.IsNullOrEmpty(ids))
            {
                return new List<int>();
            }

            List<int> lst = new List<int>();
            try
            {
                ids.Split(',').ToList().ForEach(id =>
                {
                    lst.Add(int.Parse(id));
                });
            }
            catch (Exception)
            { }
            return lst;
        }

        public static int ConvertStringToInt(string id)
        {
            try
            {
                return int.Parse(id);
            }
            catch (Exception)
            { }
            return -1;
        }

        public static string GetCodeFromName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            return name.UnicodeToASCII().Trim().ToLower().Replace(" ", "");
        }

        public static bool IsEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
            return Regex.IsMatch(email, REGEX_PATTERN_CHECK_EMAIL, RegexOptions.IgnoreCase);
        }

        public static bool IsPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return false;
            return Regex.IsMatch(phone, REGEX_PATTERN_CHECK_PHONE_NUMBER, RegexOptions.IgnoreCase);
        }

        public static string TrimJson(string json)
        {
            if (!string.IsNullOrEmpty(json))
                return json.Trim('{').Trim('}');
            return "";
        }

        public static string GenarateRandomNumber()
        {
            // Step 1: Random number
            var randomNumberWithDate = new Random((int)DateTime.Now.Ticks)
                .Next(1, 1000000)
                .ToString();
            return String.Format("{0}{1}", DateTime.Now.ToString("yyyyMMdd"), randomNumberWithDate);
        }

        public static bool CheckNumberFromString(string value)
        {
            try
            {
                int.Parse(value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static List<Guid> ParseGuid(List<string> list)
        {
            List<Guid> guids = new List<Guid>();
            foreach (var id in list)
            {
                if (IsGuid(id))
                    guids.Add(System.Guid.Parse(id));
            }
            return guids;
        }

        public static List<string> ParseString(List<Guid> list)
        {
            List<string> guids = new List<string>();
            foreach (var guid in list)
            {
                guids.Add(guid.ToString());
            }
            return guids;
        }

        public static bool IsGuid(string guid)
        {
            try
            {
                Guid.Parse(guid);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region  export excel 
        public static byte[] Export2XLS(DataTable dtData, string sheetName)
        {
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
            using (OfficeOpenXml.ExcelPackage pck = new OfficeOpenXml.ExcelPackage())
            {
                //pck.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
                //Create the worksheet
                OfficeOpenXml.ExcelWorksheet ws = pck.Workbook.Worksheets.Add(sheetName);

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dtData, true);

                //Format the header for column 1-3
                using (OfficeOpenXml.ExcelRange rng = ws.Cells["A1:BZ1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;   //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                }

                for (int i = 0; i < dtData.Columns.Count; i++)
                {
                    if (dtData.Columns[i].DataType == typeof(DateTime))
                    {
                        using (OfficeOpenXml.ExcelRange col = ws.Cells[2, i + 1, 2 + dtData.Rows.Count, i + 1])
                        {
                            //col.Style.Numberformat.Format = "MM/dd/yyyy HH:mm";
                            col.Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
                            //col.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        }
                    }
                    if (dtData.Columns[i].DataType == typeof(TimeSpan))
                    {
                        using (OfficeOpenXml.ExcelRange col = ws.Cells[2, i + 1, 2 + dtData.Rows.Count, i + 1])
                        {
                            col.Style.Numberformat.Format = "d.hh:mm";
                            col.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }
                    }

                    //if (dtData.Columns[i].DataType == typeof(double))
                    //{
                    //    using (OfficeOpenXml.ExcelRange col = ws.Cells[2, i + 1, 2 + dtData.Rows.Count, i + 1])
                    //    {
                    //        col.Style.Numberformat.Format = "G";
                    //        //col.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    //    }
                    //}
                }
                ////Example how to Format Column 1 as numeric 
                //using (OfficeOpenXml.ExcelRange col = ws.Cells[2, 1, 2 + dtData.Rows.Count, 1])
                //{
                //    col.Style.Numberformat.Format = "#,##0.00";
                //    col.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                //}

                return pck.GetAsByteArray();
            } // end using
        }

        public static DataSet ToDataSet<T>(IList<T> list)
        {
            Type elementType = typeof(T);
            DataSet ds = new DataSet();
            DataTable t = new DataTable();
            ds.Tables.Add(t);

            //add a column to table for each public property on T
            foreach (var propInfo in elementType.GetProperties())
            {
                Type ColType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

                t.Columns.Add(propInfo.Name, ColType);
            }

            //go through each property on T and add each value to the table
            foreach (T item in list)
            {
                DataRow row = t.NewRow();

                foreach (var propInfo in elementType.GetProperties())
                {
                    row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value;
                }

                t.Rows.Add(row);
            }

            return ds;
        }
        #endregion

        public static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out int bytesParsed);
        }

        public static string Base64ToString(string base64)
        {
            var bytes = System.Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static bool IsNumber(string value)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return Int32.TryParse(value.Trim(), out int _);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Md5(string input, bool isLowercase = false)
        {
            using (var md5 = MD5.Create())
            {
                var byteHash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                var hash = BitConverter.ToString(byteHash).Replace("-", "");
                return (isLowercase) ? hash.ToLower() : hash;
            }
        }

        public static long CreateTime(long time)
        {
            var result = time / 10000000 - 62135596800;
            return result < 0 ? 0 : result;
        }

        public static T GetEmptyObject<T>()
        {
            return Enumerable.Empty<T>().FirstOrDefault();
        }

        private static T GetItem<T>(DataRow dr)
        {
            Type type = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in type.GetProperties())
                {
                    try
                    {
                        if (string.Equals(pro.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase))
                        {
                            object data = dr[column.ColumnName];

                            bool notNull = data != null;
                            bool notNullEmpty = data?.ToString() != "";
                            if (Nullable.GetUnderlyingType(pro.PropertyType) != null)
                            {
                                if ((pro.PropertyType == typeof(Int16?)
                                || pro.PropertyType == typeof(Int32?)
                                || pro.PropertyType == typeof(Single?)
                                || pro.PropertyType == typeof(Double?)
                                || pro.PropertyType == typeof(Decimal?)
                                || pro.PropertyType == typeof(DateTime?)
                                || pro.PropertyType == typeof(Boolean?))
                                && notNullEmpty == false)
                                {
                                    continue;
                                }

                                if (notNullEmpty)
                                {
                                    if (pro.PropertyType == typeof(DateTime?))
                                    {
                                        pro.SetValue(obj, Convert.ChangeType(data, typeof(DateTime)), null);
                                        continue;
                                    }

                                    else if (pro.PropertyType == typeof(Int16?))
                                    {
                                        pro.SetValue(obj, Convert.ChangeType(data, typeof(Int16)), null);
                                        continue;
                                    }

                                    else if (pro.PropertyType == typeof(Int32?))
                                    {
                                        pro.SetValue(obj, Convert.ChangeType(data, typeof(Int32)), null);
                                        continue;
                                    }

                                    else if (pro.PropertyType == typeof(Single?))
                                    {
                                        bool convert = Single.TryParse(data.ToString(), out Single result);
                                        pro.SetValue(obj, result, null);
                                        continue;
                                    }

                                    else if (pro.PropertyType == typeof(Double?))
                                    {
                                        bool convert = Double.TryParse(data.ToString(), out Double result);
                                        pro.SetValue(obj, result, null);
                                        continue;
                                    }

                                    else if (pro.PropertyType == typeof(Decimal?))
                                    {
                                        bool convert = Decimal.TryParse(data.ToString(), out Decimal result);
                                        pro.SetValue(obj, result, null);
                                        continue;
                                    }
                                    else if (pro.PropertyType == typeof(Boolean?))
                                    {
                                        pro.SetValue(obj, Convert.ChangeType(data, typeof(Boolean)), null);
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                if (notNull && pro.PropertyType == typeof(String))
                                {
                                    pro.SetValue(obj, data.ToString().Trim(), null);
                                    continue;
                                }

                                if (notNullEmpty)
                                {
                                    if (pro.PropertyType == typeof(Single))
                                    {
                                        bool convert = Single.TryParse(data.ToString(), out Single result);
                                        pro.SetValue(obj, result, null);
                                        continue;
                                    }
                                    else if (pro.PropertyType == typeof(Double))
                                    {
                                        bool convert = Double.TryParse(data.ToString(), out Double result);
                                        pro.SetValue(obj, result, null);
                                        continue;
                                    }
                                    else if (pro.PropertyType == typeof(Decimal))
                                    {
                                        bool convert = Decimal.TryParse(data.ToString(), out Decimal result);
                                        pro.SetValue(obj, result, null);
                                        continue;
                                    }
                                    else
                                    {
                                        pro.SetValue(obj, Convert.ChangeType(data, pro.PropertyType), null);
                                        continue;
                                    }
                                }

                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Error Column: {column.ColumnName} || " + exception.Message);
                    }
                }
            }
            return obj;
        }


        public static List<T> ConvertDataTableToList<T>(DataTable dataTable)
        {
            List<T> data = new List<T>();
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    var result = GetItem<T>(row);
                    data.Add(result);
                }
            }
            return data;
        }

        public static int GetInt(int? value)
        {
            return value.HasValue ? value.Value : 0;
        }

        public static string GetStringFromChacractorCanbeSplitBy(this string source, string splitBy, int index, bool? isTrim = false)
        {
            string indexParts = string.Empty;
            string[] parts = source.Split(splitBy);
            indexParts = parts[index];
            indexParts = isTrim.Value! ? indexParts.Trim() : indexParts;
            return indexParts;
        }

        public static string GenerateAdvanceRequest(int typeId)
        {
            string genId = Utils.GenerateString(8);
            if (typeId == 5)
                return $"RAD{DateTime.Now.ToString("yyyyMM")}{genId}";

            return $"RSE{DateTime.Now.ToString("yyyyMM")}{genId}";
        }

        public static DateTime ChangeUnspecifiedTime(DateTime time)
        {
            return DateTime.SpecifyKind(time, DateTimeKind.Unspecified);
        }

    }

    public class EnumDescription
    {
        public int Id { get; set; }

        public string Value { get; set; } = String.Empty;
    }

   
}
