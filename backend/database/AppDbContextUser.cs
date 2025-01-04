using backend.entities;
using Microsoft.EntityFrameworkCore;

namespace backend.database;

public sealed partial class AppDbContext
{
    public DbSet<User> Users { get; set; }
    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Credentials)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);
    }
}