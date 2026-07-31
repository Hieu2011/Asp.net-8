using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ML
{
    public class SystemLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [BsonElement("level")]
        public string Level { get; set; } = string.Empty; // Information, Warning, Error, Critical

        [BsonElement("category")]
        public string Category { get; set; } = string.Empty; // Class name (e.g., "Core.Database.RedisConnectionService")

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("exception")]
        public string? Exception { get; set; }

        [BsonElement("stack_trace")]
        public string? StackTrace { get; set; }

        [BsonElement("event_id")]
        public int EventId { get; set; }

        [BsonElement("scope_data")]
        public Dictionary<string, object>? ScopeData { get; set; }

        [BsonElement("machine_name")]
        public string MachineName { get; set; } = Environment.MachineName;

        [BsonElement("application")]
        public string Application { get; set; } = "WebApiNet8";
    }
}