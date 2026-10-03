using ApiCore8.Domain.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace ApiCore8.Domain.Entities
{
    /// <summary>
    /// Mirrors the document shape written by Serilog.Sinks.MongoDB (WriteTo.MongoDBBson) —
    /// field names must match the sink's actual output, not an arbitrary custom schema.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SystemLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("UtcTimeStamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("Level")]
        public string Level { get; set; } = string.Empty; // Verbose, Debug, Information, Warning, Error, Fatal

        [BsonElement("RenderedMessage")]
        public string Message { get; set; } = string.Empty;

        // Serilog.Sinks.MongoDB lưu Exception dưới dạng document con có cấu trúc
        // (Type, Message, StackTraceString...), không phải string thường.
        [BsonElement("Exception")]
        [JsonConverter(typeof(BsonDocumentJsonConverter))]
        public BsonDocument? Exception { get; set; }

        // Structured log properties (SourceContext, and anything from Enrich.WithProperty,
        // e.g. Application from appsettings.json's Serilog:Properties section). Category/Application
        // live in here as nested fields (Properties.SourceContext / Properties.Application) rather
        // than as top-level columns, since the sink doesn't write those as first-class fields.
        [BsonElement("Properties")]
        [JsonConverter(typeof(BsonDocumentJsonConverter))]
        public BsonDocument? Properties { get; set; }
    }
}
