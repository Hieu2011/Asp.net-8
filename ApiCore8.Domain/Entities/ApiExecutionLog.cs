using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore8.Domain.Entities
{
    public class ApiExecutionLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ApiName { get; set; } = string.Empty;
        public string ClientIP { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;

        public string RequestBody { get; set; } = string.Empty;
        public string ResponseBody { get; set; } = string.Empty;

        public DateTime? StartTime { get; set; }
        public string StartTimeStr { get; set; } = string.Empty;
        public DateTime? EndTime { get; set; }
        public string EndTimeStr { get; set; } = string.Empty;
        public long ExecutionMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
