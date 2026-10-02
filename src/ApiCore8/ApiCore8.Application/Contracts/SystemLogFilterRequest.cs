namespace ApiCore8.Application.Contracts
{
    public class SystemLogFilterRequest
    {
        public string? Id { get; set; }
        public string? Level { get; set; } // Information, Warning, Error, Critical, Debug
        public string? Category { get; set; }
        public string? Message { get; set; } // Text search
        public string? Application { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "timestamp"; // timestamp, level, category
        public string SortOrder { get; set; } = "desc"; // asc, desc
    }
}