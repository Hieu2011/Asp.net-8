using AuthService.Application.Contracts;

namespace AuthService.Application.Interfaces
{
    /// <summary>Public key ký JWT (khóa hiện tại + khóa cũ còn hiệu lực khi xoay khóa). Implement ở Infrastructure.</summary>
    public interface IJwksProvider
    {
        JwksResponse GetJwks();
    }
}
