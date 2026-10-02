using Shared.DataAccess.Abstractions;
using AuthService.Application.Interfaces;
using AuthService.Domain;

namespace AuthService.Infrastructure.Postgres
{
    /// <inheritdoc cref="IClientRepository" />
    public class ClientRepository : IClientRepository
    {
        private readonly IDataCore _db;

        public ClientRepository(IDataCore db)
        {
            _db = db;
        }

        public async Task<Client> CreateAsync(Client client, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@client_id", client.ClientId);
            _db.AddParameter("@client_name", client.ClientName);
            _db.AddParameter("@client_secret_hash", client.ClientSecretHash);
            _db.AddParameter("@allowed_scopes", client.AllowedScopes);
            _db.AddParameter("@allowed_ips", client.AllowedIps);
            _db.AddParameter("@created_user", client.CreatedUser);

            return await _db.ExecStoreObjectFastAsync<Client>("sp_client_create", cancellationToken);
        }

        public async Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@client_id", clientId);
            var client = await _db.ExecStoreObjectFastAsync<Client>("sp_client_get_by_client_id", cancellationToken);
            return client.Id == Guid.Empty ? null : client;
        }

        public Task<List<Client>> GetAllAsync(CancellationToken cancellationToken = default)
            => _db.ExecStoreToListObjectAsync<Client>("sp_client_get_all", cancellationToken);

        public async Task<bool> RegenerateSecretAsync(Guid id, string newSecretHash, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@new_secret_hash", newSecretHash);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_client_regenerate_secret", cancellationToken);
            return ParseBoolResult(result);
        }

        public async Task<bool> SetActiveAsync(Guid id, bool isActive, string updatedUser, CancellationToken cancellationToken = default)
        {
            _db.AddParameter("@id", id);
            _db.AddParameter("@is_active", isActive);
            _db.AddParameter("@updated_user", updatedUser);

            var result = await _db.ExecuteNonQueryAsStringAsync("sp_client_set_active", cancellationToken);
            return ParseBoolResult(result);
        }

        private static bool ParseBoolResult(string result) => result.Trim() switch
        {
            "1" => true,
            "0" => false,
            _ => bool.TryParse(result, out var success) && success
        };
    }
}
