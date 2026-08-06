using HelpDesk_System.Db;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_System.Services;

public class RegistrationRequestService
{
    private readonly IDbContextFactory<HelpDeskDbContext> _contextFactory;

    public RegistrationRequestService(IDbContextFactory<HelpDeskDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> SubmitAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        UserRole requestedRole)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var userExists = await context.Users
            .AnyAsync(user => user.Email == normalizedEmail);

        if (userExists)
        {
            return false;
        }

        var pendingRequestExists = await context.RegistrationRequests
            .AnyAsync(request =>
                request.Email == normalizedEmail &&
                request.Status == RegistrationRequestStatus.Pending);

        if (pendingRequestExists)
        {
            return false;
        }

        var request = new RegistrationRequest
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RequestedRole = requestedRole
        };

        context.RegistrationRequests.Add(request);
        await context.SaveChangesAsync();

        return true;
    }
}