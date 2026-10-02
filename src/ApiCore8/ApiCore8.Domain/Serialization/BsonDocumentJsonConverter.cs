using MongoDB.Bson;
using MongoDB.Bson.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiCore8.Domain.Serialization
{
    /// <summary>
    /// System.Text.Json (bộ serialize JSON mặc định của ASP.NET Core) không biết cách
    /// serialize BsonDocument — nó cố đọc property qua reflection như 1 object C# thường,
    /// đụng vào field nội bộ kiểu BsonValue rồi cast sai kiểu, crash. Converter này chuyển
    /// BsonDocument sang JSON thật (qua MongoDB.Bson's ToJson) rồi ghi lại vào writer.
    /// </summary>
    public class BsonDocumentJsonConverter : JsonConverter<BsonDocument>
    {
        public override BsonDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException("Đọc BsonDocument từ JSON (request body) hiện chưa cần dùng tới.");

        public override void Write(Utf8JsonWriter writer, BsonDocument? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var json = value.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson });
            using var parsed = JsonDocument.Parse(json);
            parsed.RootElement.WriteTo(writer);
        }
    }
}
