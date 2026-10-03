using AuthService.Domain;

namespace AuthService.Application.Interfaces
{
    /// <summary>Bảng clients, Flow B — implement ở Infrastructure, gọi sp_client_* (PLAN.md mục 6).</summary>
    public interface IClientRepository
    {
        Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default);
        Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
        Task<List<Client>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> RegenerateSecretAsync(Guid id, string newSecretHash, string updatedUser, CancellationToken cancellationToken = default);
        Task<bool> SetActiveAsync(Guid id, bool isActive, string updatedUser, CancellationToken cancellationToken = default);
    }
}
