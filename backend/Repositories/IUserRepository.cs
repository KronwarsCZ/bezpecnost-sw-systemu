using backend.entities;

namespace backend.Repositories;

public interface IUserRepository
{
    public Task<User?> GetUserAsync(string username, CancellationToken cancellationToken = default);
    public Task<User?> AddUserAsync(User user, CancellationToken cancellationToken = default);

    public Task<Credential?> AddCredentialsToUserAsync(string username, Credential creds,
        CancellationToken cancellationToken);
    
    public Task<IEnumerable<Credential>?> GetCredentialsByUserAsync(string username, CancellationToken cancellationToken);
}