
using HelpDesk_System.Models.Enums;

namespace HelpDesk_System.Models;

public class Ticket
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ProblemType ProblemType { get; set; }
    public WorkImpact WorkImpact { get; set; }
    public AffectedPeople AffectedPeople { get; set; }

    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public List<TicketResponse> Responses { get; set; } = [];
}