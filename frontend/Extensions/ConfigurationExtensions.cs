using Npgsql;

namespace frontend.Extensions;

public static class ConfigurationExtensions
{
    public static string GetDbConnectionString(this IConfiguration configuration)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = configuration["Database:Host"],
            Port = int.Parse(configuration["Database:Port"]!),
            Database = configuration["Database:Database"],
            Username = configuration["Database:Username"],
            Password = configuration["Database:Password"],
        };
        return connectionStringBuilder.ConnectionString;
    }
}