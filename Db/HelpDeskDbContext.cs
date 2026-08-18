using HelpDesk_System.Models;
using HelpDesk_System.Models.Enums;
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
    public DbSet<TicketHistoryEntry> TicketHistoryEntries => Set<TicketHistoryEntry>();
    public DbSet<RegistrationRequest> RegistrationRequests => Set<RegistrationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>()
            .HasOne(ticket => ticket.Author)
            .WithMany(user => user.Tickets)
            .HasForeignKey(ticket => ticket.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Ticket>()
            .HasOne(ticket => ticket.AssignedAdmin)
            .WithMany(user => user.AssignedTickets)
            .HasForeignKey(ticket => ticket.AssignedAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TicketHistoryEntry>()
            .HasOne(entry => entry.Ticket)
            .WithMany(ticket => ticket.HistoryEntries)
            .HasForeignKey(entry => entry.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketHistoryEntry>()
            .HasOne(entry => entry.Actor)
            .WithMany()
            .HasForeignKey(entry => entry.ActorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RegistrationRequest>()
            .HasIndex(request => request.Email)
            .IsUnique()
            .HasFilter("\"Status\" = 0");
    }
}
