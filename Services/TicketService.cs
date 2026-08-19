using HelpDesk_System.Db;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_System.Services;

public class TicketService
{
    private const string TextSearchConfiguration = "simple";

    private readonly IDbContextFactory<HelpDeskDbContext> _contextFactory;
    private readonly TicketPriorityCalculator _priorityCalculator;

    public TicketService(
        IDbContextFactory<HelpDeskDbContext> contextFactory,
        TicketPriorityCalculator priorityCalculator)
    {
        _contextFactory = contextFactory;
        _priorityCalculator = priorityCalculator;
    }

    public async Task CreateTicketAsync(
        int authorId,
        string title,
        string description,
        ProblemType problemType,
        WorkImpact workImpact,
        AffectedPeople affectedPeople)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var ticket = new Ticket
        {
            AuthorId = authorId,
            Title = title.Trim(),
            Description = description.Trim(),
            ProblemType = problemType,
            WorkImpact = workImpact,
            AffectedPeople = affectedPeople,
            Priority = _priorityCalculator.Calculate(
                problemType,
                workImpact,
                affectedPeople),
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        ticket.HistoryEntries.Add(new TicketHistoryEntry
        {
            ActorId = authorId,
            Description = "Ticket created."
        });

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();
    }

    public async Task<List<Ticket>> GetUserTicketsAsync(int authorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.AssignedAdmin)
            .Include(ticket => ticket.Responses
                .OrderByDescending(response => response.CreatedAt))
            .ThenInclude(response => response.Author)
            .Include(ticket => ticket.HistoryEntries
                .OrderByDescending(entry => entry.CreatedAt))
            .ThenInclude(entry => entry.Actor)
            .Where(ticket => ticket.AuthorId == authorId)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteOpenTicketAsync(int ticketId, int authorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var deletedRows = await context.Tickets
            .Where(ticket =>
                ticket.Id == ticketId &&
                ticket.AuthorId == authorId &&
                ticket.Status == TicketStatus.Open &&
                ticket.AssignedAdminId == null)
            .ExecuteDeleteAsync();

        return deletedRows == 1;
    }

    public async Task<List<Ticket>> GetOpenTicketsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Author)
            .Include(ticket => ticket.Responses
                .OrderByDescending(response => response.CreatedAt))
            .ThenInclude(response => response.Author)
            .Include(ticket => ticket.HistoryEntries
                .OrderByDescending(entry => entry.CreatedAt))
            .ThenInclude(entry => entry.Actor)
            .Where(ticket => ticket.Status == TicketStatus.Open)
            .OrderByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetAssignedTicketsAsync(int adminId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Author)
            .Include(ticket => ticket.Responses
                .OrderByDescending(response => response.CreatedAt))
            .ThenInclude(response => response.Author)
            .Include(ticket => ticket.HistoryEntries
                .OrderByDescending(entry => entry.CreatedAt))
            .ThenInclude(entry => entry.Actor)
            .Where(ticket =>
                ticket.AssignedAdminId == adminId &&
                ticket.Status == TicketStatus.InProgress)
            .OrderByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetKnowledgeBaseTicketsAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var tickets = context.Tickets
            .AsNoTracking()
            .Where(ticket =>
                ticket.Status == TicketStatus.InProgress ||
                ticket.Status == TicketStatus.Resolved ||
                ticket.Status == TicketStatus.Closed ||
                ticket.Status == TicketStatus.Declined);

        tickets = ApplyKnowledgeBaseSearch(tickets, searchText);

        return await tickets
            .Include(ticket => ticket.Author)
            .Include(ticket => ticket.AssignedAdmin)
            .Include(ticket => ticket.Responses
                .OrderByDescending(response => response.CreatedAt))
            .ThenInclude(response => response.Author)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Ticket>> GetPublishedKnowledgeBaseTicketsAsync(
        string? searchText = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var tickets = context.Tickets
            .AsNoTracking()
            .Where(ticket =>
                ticket.IsKnowledgeBasePublished &&
                (ticket.Status == TicketStatus.Resolved ||
                 ticket.Status == TicketStatus.Closed));

        tickets = ApplyKnowledgeBaseSearch(tickets, searchText);

        return await tickets
            .Include(ticket => ticket.Responses
                .OrderByDescending(response => response.CreatedAt))
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Ticket> ApplyKnowledgeBaseSearch(
        IQueryable<Ticket> tickets,
        string? searchText)
    {
        var normalizedSearch = searchText?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return tickets;
        }

        var ticketId = int.TryParse(normalizedSearch, out var parsedTicketId)
            ? parsedTicketId
            : -1;

        return tickets.Where(ticket =>
            ticket.Id == ticketId ||
            EF.Functions.ToTsVector(
                    TextSearchConfiguration,
                    (ticket.Title ?? string.Empty) + " " +
                    (ticket.Description ?? string.Empty))
                .Matches(EF.Functions.WebSearchToTsQuery(
                    TextSearchConfiguration,
                    normalizedSearch)) ||
            ticket.Responses.Any(response =>
                EF.Functions.ToTsVector(
                        TextSearchConfiguration,
                        response.Message ?? string.Empty)
                    .Matches(EF.Functions.WebSearchToTsQuery(
                        TextSearchConfiguration,
                        normalizedSearch))));
    }

    public async Task<bool> TakeTicketAsync(int ticketId, int adminId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var changedRows = await context.Tickets
            .Where(ticket =>
                ticket.Id == ticketId &&
                ticket.Status == TicketStatus.Open &&
                ticket.AssignedAdminId == null)
            .ExecuteUpdateAsync(update => update
                .SetProperty(ticket => ticket.AssignedAdminId, adminId)
                .SetProperty(ticket => ticket.Status, TicketStatus.InProgress));

        if (changedRows != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        context.TicketHistoryEntries.Add(CreateHistoryEntry(
            ticketId,
            adminId,
            "Status changed from Open to In progress. Ticket assigned to specialist."));

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> SetKnowledgeBasePublicationAsync(
        int ticketId,
        bool isPublished)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var changedRows = await context.Tickets
            .Where(ticket =>
                ticket.Id == ticketId &&
                (ticket.Status == TicketStatus.Resolved ||
                 ticket.Status == TicketStatus.Closed))
            .ExecuteUpdateAsync(update => update.SetProperty(
                ticket => ticket.IsKnowledgeBasePublished,
                isPublished));

        return changedRows == 1;
    }

    public async Task<bool> CompleteTicketAsync(
        int ticketId,
        int adminId,
        string resolution)
    {
        resolution = resolution.Trim();

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var changedRows = await context.Tickets
            .Where(ticket =>
                ticket.Id == ticketId &&
                ticket.AssignedAdminId == adminId &&
                ticket.Status == TicketStatus.InProgress)
            .ExecuteUpdateAsync(update => update
                .SetProperty(ticket => ticket.Status, TicketStatus.Resolved));

        if (changedRows != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(resolution))
        {
            context.TicketResponses.Add(new TicketResponse
            {
                TicketId = ticketId,
                AuthorId = adminId,
                Message = resolution
            });
        }

        context.TicketHistoryEntries.Add(CreateHistoryEntry(
            ticketId,
            adminId,
            "Status changed from In progress to Resolved."));

        await context.SaveChangesAsync();

        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> CloseResolvedTicketAsync(int ticketId, int authorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var changedRows = await context.Tickets
            .Where(ticket =>
                ticket.Id == ticketId &&
                ticket.AuthorId == authorId &&
                ticket.Status == TicketStatus.Resolved)
            .ExecuteUpdateAsync(update => update
                .SetProperty(ticket => ticket.Status, TicketStatus.Closed));

        if (changedRows != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        context.TicketHistoryEntries.Add(CreateHistoryEntry(
            ticketId,
            authorId,
            "Status changed from Resolved to Closed."));

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> DeclineTicketAsync(int ticketId, int adminId, string reason)
    {
        reason = reason.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var changedRows = await context.Tickets
            .Where(ticket =>
                ticket.Id == ticketId &&
                ticket.Status == TicketStatus.Open &&
                ticket.AssignedAdminId == null)
            .ExecuteUpdateAsync(update => update.SetProperty(ticket => ticket.Status, TicketStatus.Declined));

        if (changedRows != 1)
        {
            await transaction.RollbackAsync();
            return false;
        }

        context.TicketResponses.Add(new TicketResponse
        {
            TicketId = ticketId,
            AuthorId = adminId,
            Message = reason
        });

        context.TicketHistoryEntries.Add(CreateHistoryEntry(
            ticketId,
            adminId,
            "Status changed from Open to Declined."));

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return true;
    }

    public async Task<bool> AddCommentAsync(int ticketId, int authorId, string message)
    {
        message = message.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var canComment = await context.Tickets.AnyAsync(ticket =>
            ticket.Id == ticketId &&
            (ticket.AuthorId == authorId || ticket.AssignedAdminId == authorId) &&
            (ticket.Status == TicketStatus.Open ||
             ticket.Status == TicketStatus.InProgress));

        if (!canComment)
        {
            return false;
        }

        context.TicketResponses.Add(new TicketResponse
        {
            TicketId = ticketId,
            AuthorId = authorId,
            Message = message
        });

        await context.SaveChangesAsync();
        return true;
    }

    private static TicketHistoryEntry CreateHistoryEntry(
        int ticketId,
        int actorId,
        string description)
    {
        return new TicketHistoryEntry
        {
            TicketId = ticketId,
            ActorId = actorId,
            Description = description
        };
    }
}
