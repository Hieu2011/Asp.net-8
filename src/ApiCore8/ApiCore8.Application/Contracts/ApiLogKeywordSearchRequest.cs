using System.ComponentModel.DataAnnotations;

namespace ApiCore8.Application.Contracts
{
    /// <summary>
    /// Search ApiExecutionLog bằng 1 từ khóa duy nhất — khớp kiểu LIKE (regex, không phân biệt
    /// hoa thường) trên bất kỳ field nào trong ApiName/RequestBody/ResponseBody (OR), kết hợp AND
    /// với khoảng ngày FromDate/ToDate nếu có truyền.
    /// </summary>
    public class ApiLogKeywordSearchRequest
    {
        [MaxLength(500)]
        public string? Keyword { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}
