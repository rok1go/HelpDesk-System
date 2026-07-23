using HelpDesk_System.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk_System.Db;

public class HelpDeskDbContext : DbContext
{
    public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketResponse> TicketResponses => Set<TicketResponse>();
}