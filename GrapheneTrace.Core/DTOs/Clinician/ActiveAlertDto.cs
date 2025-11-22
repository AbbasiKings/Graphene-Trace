using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class ActiveAlertDto
{
    public Guid AlertId { get; set; }
    public Guid? PatientDataId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public AlertStatus Status { get; set; }
    public string? ClinicianNotes { get; set; }
}






