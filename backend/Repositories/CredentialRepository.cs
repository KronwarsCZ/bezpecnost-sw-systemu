using backend.database;
using backend.entities;
using Microsoft.EntityFrameworkCore;
using backend.helpers;


namespace backend.Repositories;
public class CredentialRepository(AppDbContext context) : ICredentialRepository
{
    AppDbContext _context = context;
    
    public async Task<int> GetCountUsersByCredentialIdAsync(byte[] credId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.CountAsync(c => c.CredentialId == credId, cancellationToken);
    }
    

    public async Task<Credential?> GetCredentialByCredentialIdAsync(byte[] credId, CancellationToken cancellationToken)
    {
        return await _context.Credentials.FirstOrDefaultAsync(c =>c.CredentialId == credId, cancellationToken);
    }

    public async Task UpdateCounterByIdAsync(Guid id, CancellationToken cancellationToken)
    {

        var cred = await _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cred is null)
        {
            return;
        }

        cred.SignCounter += 1;
        //_context.Credentials.Update(cred);
        await _context.SaveChangesAsync(cancellationToken);
        return;
    }

    public async Task<List<Credential>?> GetCredentialsForUser(Guid userId, CancellationToken cancellationToken)
    {
        var credentials = await _context.Credentials.Where(c => c.UserId == userId).ToListAsync(cancellationToken);
        
        return credentials.Count == 0 ? null : credentials;
    }
}