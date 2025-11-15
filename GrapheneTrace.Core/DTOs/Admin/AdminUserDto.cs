using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Guid? AssignedClinicianId { get; set; }
    public string? AssignedClinicianName { get; set; }
}

