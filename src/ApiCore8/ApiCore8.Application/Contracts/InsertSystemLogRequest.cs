namespace ApiCore8.Application.Contracts
{
    public class InsertSystemLogRequest
    {
        public string Level { get; set; } = "Information";
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
    }
}
