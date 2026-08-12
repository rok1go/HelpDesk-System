using HelpDesk_System.Db;
using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_System.Services;

public class TicketService
{
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

		context.Tickets.Add(ticket);
		await context.SaveChangesAsync();
	}

	public async Task<List<Ticket>> GetUserTicketsAsync(int authorId)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		return await context.Tickets
			.AsNoTracking()
			.Include(ticket => ticket.AssignedAdmin)
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
			.Where(ticket => ticket.AssignedAdminId == adminId && ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Declined)
			.OrderByDescending(ticket => ticket.Priority)
			.ThenBy(ticket => ticket.CreatedAt)
			.ToListAsync();
	}

	public async Task<bool> TakeTicketAsync(int ticketId, int adminId)
	{
		await using var context = await _contextFactory.CreateDbContextAsync();

		var changedRows = await context.Tickets
			.Where(ticket => ticket.Id == ticketId && ticket.Status == TicketStatus.Open && ticket.AssignedAdminId == null)
			.ExecuteUpdateAsync(update => update
				.SetProperty(ticket => ticket.AssignedAdminId, adminId)
				.SetProperty(ticket => ticket.Status, TicketStatus.InProgress));

		return changedRows == 1;
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
			.Where(ticket => ticket.Id == ticketId && ticket.Status == TicketStatus.Open && ticket.AssignedAdminId == null)
			.ExecuteUpdateAsync(update => update.SetProperty(ticket => ticket.Status, TicketStatus.Declined));

		if (changedRows != 1)
		{
			await transaction.RollbackAsync();
			return false;
		}

		context.TicketResponses.Add(new TicketResponse { TicketId = ticketId, AuthorId = adminId, Message = reason });
		await context.SaveChangesAsync();
		await transaction.CommitAsync();

		return true;
	}
}
