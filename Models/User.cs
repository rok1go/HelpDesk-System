using HelpDesk_System.Models.Enums;

namespace HelpDesk_System.Models;

public class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Worker;

    public List<Ticket> Tickets { get; set; } = [];
}