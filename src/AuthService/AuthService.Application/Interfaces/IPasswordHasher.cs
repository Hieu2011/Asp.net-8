namespace AuthService.Application.Interfaces
{
    /// <summary>BCrypt.Net-Next (PLAN.md mục 1). Dùng chung cho password user lẫn client_secret Flow B.</summary>
    public interface IPasswordHasher
    {
        string Hash(string plainText);
        bool Verify(string plainText, string hash);
    }
}
