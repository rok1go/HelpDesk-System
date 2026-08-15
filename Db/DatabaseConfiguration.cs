using Microsoft.Extensions.Configuration;

namespace HelpDesk_System.Db;

public static class DatabaseConfiguration
{
    private const string ConnectionStringName = "DefaultConnection";

    public static string GetRequiredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection is not configured. " +
                "Set the ConnectionStrings__DefaultConnection environment variable.");
        }

        return connectionString;
    }
}
