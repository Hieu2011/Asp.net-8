using ApiCore8.Domain.Entities;

namespace ApiCore8.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<Users> CreateAsync(Users user, CancellationToken cancellationToken = default);
        Task<Users?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Users>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm user tạo trong khoảng [fromDateUtc, toDateUtc] — 2 mốc PHẢI là UTC (đã convert từ
        /// DateTimeOffset ở tầng Controller, tránh mơ hồ Kind khi nhận trực tiếp DateTime từ client).
        /// </summary>
        Task<List<Users>> SearchByCreatedDateAsync(DateTime fromDateUtc, DateTime toDateUtc, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Users user, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
