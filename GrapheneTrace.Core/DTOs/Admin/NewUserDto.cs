using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Admin;

public class NewUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public Guid? AssignedClinicianId { get; set; }
}

