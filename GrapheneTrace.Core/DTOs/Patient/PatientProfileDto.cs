namespace GrapheneTrace.Core.DTOs.Patient;

public class PatientProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public AssignedClinicianDto? AssignedClinician { get; set; }
}

public class AssignedClinicianDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Specialization { get; set; }
    public string? ClinicName { get; set; }
}

