using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Core
{
    public static class StringUtilities
    {
        private static readonly Random _random = new Random();

        private static readonly int[] _UnicodeCharactersList = Enumerable.Range(48, 10).Concat(Enumerable.Range(65, 26)).ToArray();

        private static readonly List<KeyValuePair<string, string>> _cyrillicCharacters = new List<KeyValuePair<string, string>>
    {
        new KeyValuePair<string, string>("\\u0400", "E"),
        new KeyValuePair<string, string>("\\u0401", "E"),
        new KeyValuePair<string, string>("\\u0405", "S"),
        new KeyValuePair<string, string>("\\u0406", "I"),
        new KeyValuePair<string, string>("\\u0407", "I"),
        new KeyValuePair<string, string>("\\u0408", "J"),
        new KeyValuePair<string, string>("\\u040E", "Y"),
        new KeyValuePair<string, string>("\\u0410", "A"),
        new KeyValuePair<string, string>("\\u0412", "B"),
        new KeyValuePair<string, string>("\\u0415", "E"),
        new KeyValuePair<string, string>("\\u041A", "K"),
        new KeyValuePair<string, string>("\\u041C", "M"),
        new KeyValuePair<string, string>("\\u041D", "H"),
        new KeyValuePair<string, string>("\\u041E", "O"),
        new KeyValuePair<string, string>("\\u0420", "P"),
        new KeyValuePair<string, string>("\\u0421", "C"),
        new KeyValuePair<string, string>("\\u0422", "T"),
        new KeyValuePair<string, string>("\\u0423", "Y"),
        new KeyValuePair<string, string>("\\u0425", "X"),
        new KeyValuePair<string, string>("\\u0432", "B"),
        new KeyValuePair<string, string>("\\u0433", "R"),
        new KeyValuePair<string, string>("\\u043A", "K"),
        new KeyValuePair<string, string>("\\u043C", "M"),
        new KeyValuePair<string, string>("\\u043D", "H"),
        new KeyValuePair<string, string>("\\u043E", "O"),
        new KeyValuePair<string, string>("\\u0440", "P"),
        new KeyValuePair<string, string>("\\u0441", "C"),
        new KeyValuePair<string, string>("\\u0442", "T"),
        new KeyValuePair<string, string>("\\u0443", "Y"),
        new KeyValuePair<string, string>("\\u0445", "X")
    };

        private static readonly string[] VietnameseSigns = new string[15]
        {
        "aAeEoOuUiIdDyY", "áàạảãâấầậẩẫăắằặẳẵ", "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ", "éèẹẻẽêếềệểễ", "ÉÈẸẺẼÊẾỀỆỂỄ", "óòọỏõôốồộổỗơớờợởỡ", "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ", "úùụủũưứừựửữ", "ÚÙỤỦŨƯỨỪỰỬỮ", "íìịỉĩ",
        "ÍÌỊỈĨ", "đ", "Đ", "ýỳỵỷỹ", "ÝỲỴỶỸ"
        };

        public static string CyrillicToASCII(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string value2 = EncodeNonAsciiCharacters(value);
            return DecodeEncodedNonAsciiCharacters(value2);
        }

        private static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(value, "\\\\u(?<Value>[a-zA-Z0-9]{4})", delegate (Match match)
            {
                string value2 = match.Groups[0].Value;
                return _cyrillicCharacters.FirstOrDefault((KeyValuePair<string, string> x) => x.Key.ToLower() == value2.ToLower()).Value.NullToEmpty();
            });
        }

        private static string EncodeNonAsciiCharacters(string value)
        {
            return Regex.Replace(value, "[^\\x00-\\x7F]", (Match m) => $"\\u{(int)m.Value[0]:X4}");
        }

        public static string GenerateOTP()
        {
            return new Random().Next(0, 999999).ToString("000000");
        }

        public static string GetKeywordElasticFullTextSearch(this string value)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string text = new string(value.Select((char x) => (!invalidChars.Contains(x)) ? x : ' ').ToArray());
            return string.Join(" ", from x in text.Split(' ')
                                    where !string.IsNullOrEmpty(x)
                                    select x into item
                                    select $"*{item}*");
        }

        public static string GetKeywordElasticFullTextSearchV2(this string value)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string text = new string(value.Select((char x) => (!invalidChars.Contains(x)) ? x : ' ').ToArray());
            string[] array = (from x in text.Split(' ')
                              where !string.IsNullOrEmpty(x)
                              select x).ToArray();
            if (array.Length != 0)
            {
                array[0] += "^2";
            }

            value = string.Join(" ", array);
            string text2 = "";
            for (int i = 0; i < array.Length; i++)
            {
                text2 = ((i >= array.Length - 1) ? (text2 + array[i]) : (text2 + array[i] + " AND "));
            }

            return "(*" + value + ") OR (" + value + "*) OR (" + text2 + ")";
        }

        public static string GetKeywordElasticFullTextSearchV3(this string value)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string text = new string(value.Select((char x) => (!invalidChars.Contains(x)) ? x : ' ').ToArray());
            return string.Join(" AND ", from x in text.Split(' ')
                                        where !string.IsNullOrEmpty(x)
                                        select x);
        }

        public static string GetKeywordElasticSearchLike(this string value)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string text = new string(value.Select((char x) => (!invalidChars.Contains(x)) ? x : ' ').ToArray());
            string[] value2 = (from x in text.Split(' ')
                               where !string.IsNullOrEmpty(x)
                               select x into item
                               select $"*{item}*").ToArray();
            return string.Join(" AND ", value2);
        }

        public static string GetValueWithoutSpecialCharacters(this string value, string separatorCharacter = "")
        {
            string pattern = "[^a-zA-Z0-9_]+";
            string text = Regex.Replace(value.UnicodeToASCII(), pattern, separatorCharacter);
            return text.ToLower();
        }

        public static string NullToEmpty(this object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            return value.ToString().Trim();
        }

        public static string RandomAppId(int length = 8)
        {
            Random random = new Random();
            return new string((from s in Enumerable.Repeat("ABCDEF0123456789", length)
                               select s[random.Next(s.Length)]).ToArray());
        }

        public static string GenarateRandomString(int maxSize = 12)
        {
            string text = new Random((int)DateTime.Now.Ticks).Next(1, 1000).ToString();
            string text2 = string.Empty;
            int num = maxSize - text.Length;
            for (int i = 0; i < num; i++)
            {
                int value = _UnicodeCharactersList[_random.Next(1, _UnicodeCharactersList.Length)];
                text2 += Convert.ToChar(value);
            }

            int startIndex = new Random().Next(0, text2.Length);
            return text2.Insert(startIndex, text);
        }

        public static string UnicodeToASCII(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string input = value.Normalize(NormalizationForm.FormD);
            return regex.Replace(input, string.Empty).Replace('đ', 'd').Replace('Đ', 'D');
        }

        public static string CreateMD5(this Stream stream)
        {
            using MD5 mD = MD5.Create();
            byte[] array = mD.ComputeHash(stream);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                stringBuilder.Append(array[i].ToString("X2").ToLower());
            }

            return stringBuilder.ToString();
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Substring(0, Math.Min(value.Length, maxLength));
            }

            return value;
        }

        public static string CreateMD5(this string input)
        {
            using MD5 mD = MD5.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(input);
            byte[] array = mD.ComputeHash(bytes);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                stringBuilder.Append(array[i].ToString("X2").ToLower());
            }

            return stringBuilder.ToString();
        }

        public static Func<HttpContext, string> ForwardReferenceToken(string authenticationScheme, string introspectionScheme = "introspection")
        {
            return Select;
            string Select(HttpContext context)
            {
                var (text, text2) = GetSchemeAndCredential(context);
                if (text.Equals(authenticationScheme, StringComparison.OrdinalIgnoreCase) && !text2.Contains("."))
                {
                    return introspectionScheme;
                }

                return null;
            }
        }

        public static (string, string) GetSchemeAndCredential(HttpContext context)
        {
            string text = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(text))
            {
                return (string.Empty, string.Empty);
            }

            string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (array.Length != 2)
            {
                return (string.Empty, string.Empty);
            }

            return (array[0], array[1]);
        }

        public static int ParseString2Int(this string str)
        {
            int result = 0;
            int.TryParse(str, out result);
            return result;
        }

        public static bool ParseString2Bool(this string str)
        {
            bool.TryParse(str, out var result);
            return result;
        }

        public static decimal ParseString2Decimal(string str)
        {
            decimal result = default(decimal);
            decimal.TryParse(str, out result);
            return result;
        }

        public static double ParseString2Double(this string value)
        {
            double result = 0.0;
            if (!string.IsNullOrEmpty(value))
            {
                double.TryParse(value, out result);
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    return 0.0;
                }

                return result;
            }

            return result;
        }

        public static string RemoveSign4VietnameseString(string str)
        {
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                {
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
                }
            }

            return str;
        }

        public static string GetRealIP(this IHeaderDictionary headers, string key)
        {
            Dictionary<string, StringValues> dictionary = headers.ToDictionary<KeyValuePair<string, StringValues>, string, StringValues>((KeyValuePair<string, StringValues> item) => item.Key.ToLower(), (KeyValuePair<string, StringValues> item) => item.Value);
            string empty = string.Empty;
            if (!dictionary.ContainsKey(key.ToLower()))
            {
                return string.Empty;
            }

            return dictionary[key.ToLower()].NullToEmpty();
        }

        public static string MaskString(this string source, int start, int maskLength, char maskCharacter = '*')
        {
            if (source == null || source.Length <= 0 || maskLength < 0)
            {
                return "";
            }

            string text = new string(maskCharacter, maskLength);
            string text2 = source.Substring(0, start);
            string text3 = source.Substring(start + maskLength, source.Length - maskLength);
            return text2 + text + text3;
        }

        public static T ConvertQueryStringToObject<T>(this string fromObject)
        {
            NameValueCollection dict = HttpUtility.ParseQueryString(fromObject);
            string value = JsonConvert.SerializeObject(dict.Cast<string>().ToDictionary((string k) => k, (string v) => dict[v]));
            return JsonConvert.DeserializeObject<T>(value);
        }

        public static T ToObject<T>(this object fromObject)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(fromObject, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }));
        }

        public static List<T> ToObjectList<T>(this object fromObject)
        {
            return JsonConvert.DeserializeObject<List<T>>(JsonConvert.SerializeObject(fromObject, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            }));
        }

        public static string ToSnakeCase(this string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (text.Length < 2)
            {
                return text;
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(char.ToLowerInvariant(text[0]));
            for (int i = 1; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsUpper(c))
                {
                    stringBuilder.Append('_');
                    stringBuilder.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        public static string RemoveDomainName(this string Url, string domainTarget = null)
        {
            Regex regex = new Regex("^((https?|ftp)://)?(www\\.)?(?<domain>[^/]+)(/|$)");
            Match match = regex.Match(Url);
            if (match.Success)
            {
                string text = match.Groups["domain"].Value;
                int num = text.Where((char x) => x == '.').Count();
                while (num > 2)
                {
                    if (num > 2)
                    {
                        string[] array = text.Split('.', 2);
                        text = array[1];
                        num = text.Where((char x) => x == '.').Count();
                    }
                }

                if (string.IsNullOrEmpty(domainTarget))
                {
                    return Url.Remove(0, match.Value.Count() - 1);
                }

                Match match2 = regex.Match(domainTarget);
                if (!match2.Success)
                {
                    return string.Empty;
                }

                if (domainTarget.Contains(text))
                {
                    return Url.Remove(0, match.Value.Count() - 1);
                }

                return Url;
            }

            return string.Empty;
        }
    }
}
