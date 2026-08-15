using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HelpDesk_System.Db;

public class HelpDeskDbContextFactory : IDesignTimeDbContextFactory<HelpDeskDbContext>
{
    public HelpDeskDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = DatabaseConfiguration.GetRequiredConnectionString(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<HelpDeskDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new HelpDeskDbContext(optionsBuilder.Options);
    }
}
