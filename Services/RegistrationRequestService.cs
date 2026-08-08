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

    public async Task<List<RegistrationRequest>> GetPendingRequestsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.RegistrationRequests
            .AsNoTracking()
            .Where(request => request.Status == RegistrationRequestStatus.Pending)
            .OrderBy(request => request.CreatedAt)
            .ToListAsync();
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

    public async Task<bool> ApproveAsync(
        int requestId,
        int adminId,
        UserRole assignedRole)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var request = await context.RegistrationRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(request =>
                request.Id == requestId &&
                request.Status == RegistrationRequestStatus.Pending);

        if (request is null)
        {
            return false;
        }

        var userExists = await context.Users
            .AnyAsync(user => user.Email == request.Email);

        if (userExists)
        {
            return false;
        }

        var processedAt = DateTime.UtcNow;
        var updatedRequests = await context.RegistrationRequests
            .Where(registrationRequest =>
                registrationRequest.Id == requestId &&
                registrationRequest.Status == RegistrationRequestStatus.Pending)
            .ExecuteUpdateAsync(update => update
                .SetProperty(registrationRequest => registrationRequest.Status, RegistrationRequestStatus.Approved)
                .SetProperty(registrationRequest => registrationRequest.AssignedRole, assignedRole)
                .SetProperty(registrationRequest => registrationRequest.ProcessedAt, processedAt)
                .SetProperty(registrationRequest => registrationRequest.ProcessedByAdminId, adminId)
                .SetProperty(registrationRequest => registrationRequest.DecisionReason, (string?)null));

        if (updatedRequests == 0)
        {
            return false;
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = request.PasswordHash,
            Role = assignedRole
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> DeclineAsync(
        int requestId,
        int adminId,
        string reason)
    {
        var normalizedReason = reason.Trim();

        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var processedAt = DateTime.UtcNow;
        var updatedRequests = await context.RegistrationRequests
            .Where(request =>
                request.Id == requestId &&
                request.Status == RegistrationRequestStatus.Pending)
            .ExecuteUpdateAsync(update => update
                .SetProperty(request => request.Status, RegistrationRequestStatus.Declined)
                .SetProperty(request => request.AssignedRole, (UserRole?)null)
                .SetProperty(request => request.ProcessedAt, processedAt)
                .SetProperty(request => request.ProcessedByAdminId, adminId)
                .SetProperty(request => request.DecisionReason, normalizedReason));

        return updatedRequests == 1;
    }
}