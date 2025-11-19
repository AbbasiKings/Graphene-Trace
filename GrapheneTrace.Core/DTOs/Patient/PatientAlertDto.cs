using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Patient;

public class PatientAlertDto
{
    public Guid AlertId { get; set; }
    public Guid PatientDataId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public AlertStatus Status { get; set; } = AlertStatus.New;
    public string? ClinicianNotes { get; set; }
    public DateTime Timestamp { get; set; }
}

