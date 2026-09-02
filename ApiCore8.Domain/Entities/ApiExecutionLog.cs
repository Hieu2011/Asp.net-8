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
        public DateTime? EndTime { get; set; }

        /// <summary>Thời gian thực thi tính bằng mili giây — giữ kiểu số để lọc/sort (VD GetSlowLogs).</summary>
        public long ExecutionMs { get; set; }

        /// <summary>
        /// Dạng hiển thị của ExecutionMs, tự quy đổi đơn vị (ms/s/min/h) kèm hậu tố — VD "707 ms",
        /// "2.35 s", "1.20 min" — tránh nhìn số mili giây trần trụi dễ hiểu nhầm đơn vị.
        /// </summary>
        public string ExecutionTimeDisplay { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
