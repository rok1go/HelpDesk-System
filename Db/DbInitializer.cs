using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpDesk_System.Db;

public class DbInitializer
{
    private readonly IDbContextFactory<HelpDeskDbContext> _contextFactory;
    private readonly IConfiguration _configuration;

    public DbInitializer(
        IDbContextFactory<HelpDeskDbContext> contextFactory,
        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        var email = _configuration["InitialAdmin:Email"];
        var password = _configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        email = email.Trim().ToLowerInvariant();

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Users.AnyAsync(user => user.Email == email))
        {
            return;
        }

        var admin = new User
        {
            FirstName = _configuration["InitialAdmin:FirstName"] ?? "Initial",
            LastName = _configuration["InitialAdmin:LastName"] ?? "Admin",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Admin
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}