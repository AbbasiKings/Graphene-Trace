using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Admin;

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public bool IsActive { get; set; } = true;
    public Guid? AssignedClinicianId { get; set; }
    public string? Password { get; set; } // Optional - only update if provided
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}




