using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.Models;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public bool IsActive { get; set; } = true;
    public Guid? AssignedClinicianId { get; set; }
    public User? AssignedClinician { get; set; }
    public ICollection<User> AssignedPatients { get; set; } = new List<User>();
    public ICollection<PatientData> PatientData { get; set; } = new List<PatientData>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public DateTime? LastLoginAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}

