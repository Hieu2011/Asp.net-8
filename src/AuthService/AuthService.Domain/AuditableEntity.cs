namespace AuthService.Domain
{
    /// <summary>
    /// 7 cột audit dùng chung cho mọi bảng (PLAN.md mục 4) — soft-delete, không hard-delete.
    /// </summary>
    public abstract class AuditableEntity
    {
        public DateTime CreatedDate { get; set; }
        public string? CreatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedUser { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string? DeletedUser { get; set; }
    }
}
