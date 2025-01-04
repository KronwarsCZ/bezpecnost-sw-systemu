using backend.database;
using backend.entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    AppDbContext _context = context;

    public async Task<User?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users.Where(u => u.Name == username).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(u => u.Name == user.Name, cancellationToken))
        {
            return null;
        }
        
        await _context.Users.AddAsync(user, cancellationToken: cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<Credential?> AddCredentialsToUserAsync(string username, Credential creds,
        CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Credentials = user.Credentials.Append(creds).ToList();
        await _context.SaveChangesAsync(cancellationToken);
        return creds;
    }

    public async Task<IEnumerable<Credential>?> GetCredentialsByUserAsync(string username, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(username, cancellationToken);
        return user?.Credentials;
    }
}