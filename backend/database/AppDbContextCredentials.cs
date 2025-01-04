using backend.entities;
using Microsoft.EntityFrameworkCore;

namespace backend.database;

public sealed partial class AppDbContext
{
    public DbSet<Credential> Credentials { get; set; }
    private void ConfigureCredentials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credential>().Property(c => c.Id).ValueGeneratedNever();
        modelBuilder.Entity<Credential>().Property(c => c.CredentialId).HasColumnType("bytea");
    }
}