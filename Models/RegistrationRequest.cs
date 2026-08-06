using HelpDesk_System.Models.Enums;

namespace HelpDesk_System.Models;

public class RegistrationRequest
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole RequestedRole { get; set; } = UserRole.Worker;
    public UserRole? AssignedRole { get; set; }

    public RegistrationRequestStatus Status { get; set; } = RegistrationRequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public int? ProcessedByAdminId { get; set; }
    public User? ProcessedByAdmin { get; set; }

    public string? DecisionReason { get; set; }
}