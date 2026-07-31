using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML
{
    public class LogFilterRequest
    {
        public string? Id { get; set; }

        [MaxLength(200)]
        public string? ApiName { get; set; }

        [MaxLength(10)]
        public string? Method { get; set; }

        [MaxLength(500)]
        public string? Keyword { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)] // ✅ Giới hạn max 100 items/page
        public int PageSize { get; set; } = 20;
    }
}
