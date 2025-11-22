namespace GrapheneTrace.Core.DTOs.Patient;

public class UpdatePatientProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}


