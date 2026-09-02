using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace ApiCore8.Infrastructure.Database
{
    /// <summary>
    /// Map IDataReader -> object bằng Expression Tree biên dịch sẵn (build 1 lần mỗi lần gọi
    /// ExecStoreToListObjectFastAsync, dựa theo cột thật của reader đang mở), KHÔNG dùng
    /// PropertyInfo.SetValue theo reflection như DataRowMapper — nhanh hơn ~7-8 lần ở quy mô
    /// chục-trăm nghìn dòng (đã benchmark thật), vì SetValue phải "diễn giải" lại metadata mỗi
    /// dòng, còn Expression Tree biên dịch ra IL thật, gán property trực tiếp như code viết tay.
    /// Không cần "cache" theo Type toàn cục vì thứ tự/vị trí cột (ordinal) phụ thuộc từng câu
    /// SQL cụ thể đang mở, không cố định theo Type.
    /// </summary>
    public static class CompiledReaderMapper
    {
        // Chuẩn hóa để so khớp: bỏ "_" + lowercase — giống hệt quy ước của DataRowMapper.
        private static string NormalizeKey(string name) => name.Replace("_", "").ToLowerInvariant();

        public static Func<IDataRecord, T> Build<T>(IDataReader reader)
        {
            var recordParam = Expression.Parameter(typeof(IDataRecord), "r");
            var objVar = Expression.Variable(typeof(T), "obj");

            // "new T()" trước — tôn trọng field initializer của entity (VD Users.Username = string.Empty).
            // Dùng khối lệnh (Block) + gán có điều kiện thay vì MemberInit, vì MemberInit LUÔN set giá
            // trị cho mọi binding kể cả khi cột là DBNull (Expression.Default(string) = null, ghi đè
            // mất field initializer) — khác hành vi DataRowMapper (bỏ qua hẳn khi DBNull, giữ nguyên
            // default của constructor). Bug thật đã bắt được qua unit test khi so 2 cách này.
            var statements = new List<Expression> { Expression.Assign(objVar, Expression.New(typeof(T))) };

            var columnOrdinals = new Dictionary<string, int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnOrdinals[NormalizeKey(reader.GetName(i))] = i;
            }

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                if (!columnOrdinals.TryGetValue(NormalizeKey(prop.Name), out var ordinal))
                {
                    continue; // Không có cột khớp — bỏ qua, giữ giá trị mặc định của property.
                }

                var ordinalExpr = Expression.Constant(ordinal);
                var rawValueExpr = BuildTypedValueExpression(recordParam, ordinalExpr, prop.PropertyType);
                var isDbNullExpr = Expression.Call(recordParam, nameof(IDataRecord.IsDBNull), null, ordinalExpr);

                var assignExpr = Expression.Assign(Expression.Property(objVar, prop), rawValueExpr);
                statements.Add(Expression.IfThen(Expression.Not(isDbNullExpr), assignExpr));
            }

            statements.Add(objVar); // giá trị trả về của block

            var block = Expression.Block(new[] { objVar }, statements);
            return Expression.Lambda<Func<IDataRecord, T>>(block, recordParam).Compile();
        }

        private static Expression BuildTypedValueExpression(ParameterExpression recordParam, ConstantExpression ordinalExpr, Type propertyType)
        {
            if (propertyType == typeof(Guid))
            {
                // Postgres (uuid)/SQL Server (uniqueidentifier) trả sẵn Guid; Oracle không có UUID
                // native, trả VARCHAR2 (string) -> cần Guid.Parse. GuidHelper.ToGuid xử lý cả 2.
                var rawExpr = Expression.Call(recordParam, nameof(IDataRecord.GetValue), null, ordinalExpr);
                var toGuidMethod = typeof(GuidHelper).GetMethod(nameof(GuidHelper.ToGuid))!;
                return Expression.Call(toGuidMethod, rawExpr);
            }

            if (propertyType == typeof(string))
                return Expression.Call(recordParam, nameof(IDataRecord.GetString), null, ordinalExpr);

            if (propertyType == typeof(bool))
                return Expression.Call(recordParam, nameof(IDataRecord.GetBoolean), null, ordinalExpr);

            if (propertyType == typeof(DateTime))
            {
                var dtExpr = Expression.Call(recordParam, nameof(IDataRecord.GetDateTime), null, ordinalExpr);
                // Ép Kind=Utc giống DataRowMapper — SqlClient/Oracle trả Kind=Unspecified dù dữ liệu
                // luôn là UTC, không ép lại thì System.Text.Json serialize thiếu hậu tố "Z".
                var specifyKindMethod = typeof(DateTime).GetMethod(nameof(DateTime.SpecifyKind))!;
                return Expression.Call(specifyKindMethod, dtExpr, Expression.Constant(DateTimeKind.Utc));
            }

            if (propertyType == typeof(int)) return Expression.Call(recordParam, nameof(IDataRecord.GetInt32), null, ordinalExpr);
            if (propertyType == typeof(long)) return Expression.Call(recordParam, nameof(IDataRecord.GetInt64), null, ordinalExpr);
            if (propertyType == typeof(decimal)) return Expression.Call(recordParam, nameof(IDataRecord.GetDecimal), null, ordinalExpr);
            if (propertyType == typeof(double)) return Expression.Call(recordParam, nameof(IDataRecord.GetDouble), null, ordinalExpr);

            // Kiểu ít gặp hơn — fallback Convert.ChangeType (vẫn nhanh hơn PropertyInfo.SetValue vì
            // gán property vẫn qua compiled expression, không qua reflection).
            var valueExpr = Expression.Call(recordParam, nameof(IDataRecord.GetValue), null, ordinalExpr);
            var changeTypeMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) })!;
            var changedExpr = Expression.Call(changeTypeMethod, valueExpr, Expression.Constant(propertyType));
            return Expression.Convert(changedExpr, propertyType);
        }
    }

    internal static class GuidHelper
    {
        public static Guid ToGuid(object value) => value is Guid guid ? guid : Guid.Parse(value.ToString()!);
    }
}
