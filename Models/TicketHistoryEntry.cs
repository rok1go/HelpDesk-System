namespace HelpDesk_System.Models;

public class TicketHistoryEntry
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int? ActorId { get; set; }
    public User? Actor { get; set; }
}
