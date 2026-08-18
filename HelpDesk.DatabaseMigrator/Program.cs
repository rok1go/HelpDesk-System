using HelpDesk_System.Db;
using Microsoft.EntityFrameworkCore;
using Npgsql;

const string connectionStringVariable = "ConnectionStrings__DefaultConnection";
const int maximumAttempts = 30;
var connectionString = Environment.GetEnvironmentVariable(connectionStringVariable);

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        $"Environment variable {connectionStringVariable} is required.");

    return 1;
}

var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
    .UseNpgsql(connectionString)
    .Options;

for (var attempt = 1; attempt <= maximumAttempts; attempt++)
{
    try
    {
        await using var context = new HelpDeskDbContext(options);
        await context.Database.MigrateAsync();

        Console.WriteLine("Database migrations applied successfully.");
        return 0;
    }
    catch (Exception exception) when (
        attempt < maximumAttempts &&
        exception is NpgsqlException or TimeoutException)
    {
        Console.WriteLine(
            $"Database is not ready. Attempt {attempt} of {maximumAttempts}.");

        await Task.Delay(TimeSpan.FromSeconds(2));
    }
}

return 1;
