namespace AuthService.Domain
{
    /// <summary>
    /// Cấp bậc RBAC — số càng nhỏ, quyền càng cao (PLAN.md mục 2).
    /// </summary>
    public enum RoleLevel
    {
        Admin = 1,
        Director = 2,
        Manager = 3,
        Staff = 4,
        Customer = 5
    }
}
