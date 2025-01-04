using backend.entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Credential = backend.entities.Credential;

namespace backend.Repositories;

public interface ICredentialRepository
{
    public Task<int> GetCountUsersByCredentialIdAsync(byte[] credId, CancellationToken cancellationToken);
    
    public Task<Credential?> GetCredentialByCredentialIdAsync(byte[] credId, CancellationToken cancellationToken);

    public Task UpdateCounterByIdAsync(Guid id, CancellationToken cancellationToken);

    public Task<List<Credential>?> GetCredentialsForUser(Guid userId, CancellationToken cancellationToken);
}