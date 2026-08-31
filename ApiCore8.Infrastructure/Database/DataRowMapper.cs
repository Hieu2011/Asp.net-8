using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace ApiCore8.Infrastructure.Database
{
    /// <summary>
    /// Ánh xạ DataRow/DataTable sang object — dùng chung cho mọi provider (Postgres/Oracle/SqlServer).
    /// Tách riêng để 3 DbHelper không phải lặp lại cùng 1 logic map.
    /// </summary>
    public static class DataRowMapper
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo[]> PropertyCache = new();

        private static PropertyInfo[] GetTypeProperties<T>()
        {
            string typeName = typeof(T).FullName!;
            return PropertyCache.GetOrAdd(typeName, _ => typeof(T).GetProperties());
        }

        // Chuẩn hóa để so khớp: bỏ "_" + lowercase — property C# PascalCase ("CreatedAt") phải khớp
        // được với cột SQL snake_case ("created_at"). Chỉ lowercase thôi (không bỏ "_") sẽ KHÔNG
        // khớp ("createdat" != "created_at") — bug thật đã gặp: created_at/updated_at/full_name/
        // password_hash/is_active toàn bộ bị bỏ qua, chỉ id/username/email (1 từ) tình cờ khớp.
        private static string NormalizeKey(string name) => name.Replace("_", "").ToLowerInvariant();

        public static T GetItem<T>(DataRow row)
        {
            T obj = Activator.CreateInstance<T>();
            var properties = GetTypeProperties<T>();

            var propertyMap = properties.ToDictionary(p => NormalizeKey(p.Name), p => p);

            foreach (DataColumn column in row.Table.Columns)
            {
                string columnKey = NormalizeKey(column.ColumnName);
                if (!propertyMap.TryGetValue(columnKey, out var property))
                {
                    continue;
                }

                try
                {
                    var data = row[column.ColumnName];
                    if (data == DBNull.Value || data?.ToString() == "")
                    {
                        continue; // Bỏ qua giá trị null
                    }

                    Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    // Convert.ChangeType không xử lý được Guid (Guid không implement IConvertible) → tự xử lý riêng.
                    // Postgres (uuid)/SQL Server (uniqueidentifier) trả sẵn System.Guid; Oracle không có kiểu
                    // UUID native, phải lưu dạng chuỗi (VARCHAR2) → trả về string, cần Guid.Parse thay vì cast thẳng.
                    object convertedValue;
                    if (propertyType == typeof(Guid))
                    {
                        convertedValue = data is Guid guidValue ? guidValue : Guid.Parse(data!.ToString()!);
                    }
                    else if (propertyType == typeof(DateTime))
                    {
                        // Toàn bộ hệ thống chỉ lưu UTC (DateTime.UtcNow/now()/SYSUTCDATETIME()/SYSTIMESTAMP)
                        // — nhưng chỉ Npgsql tự gắn Kind=Utc cho cột timestamptz; SqlClient/Oracle trả về
                        // Kind=Unspecified dù dữ liệu vẫn là UTC. Nếu không ép lại, System.Text.Json sẽ
                        // serialize thiếu hậu tố "Z" cho SQL Server/Oracle → client (JS) hiểu nhầm thành
                        // giờ local của máy client, convert sai lần 2 (chồng UTC lên UTC coi như local).
                        var dt = (DateTime)Convert.ChangeType(data, typeof(DateTime));
                        convertedValue = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(data, propertyType);
                    }

                    property.SetValue(obj, convertedValue, null);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error Column: {column.ColumnName} || {ex.Message}", ex);
                }
            }

            return obj;
        }

        public static List<T> ConvertDataTableToList<T>(DataTable table)
        {
            var result = new List<T>();
            foreach (DataRow row in table.Rows)
            {
                result.Add(GetItem<T>(row));
            }
            return result;
        }
    }
}
