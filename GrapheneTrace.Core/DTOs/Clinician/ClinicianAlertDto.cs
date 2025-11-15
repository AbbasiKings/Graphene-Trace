using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class ClinicianAlertDto
{
    public Guid AlertId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public AlertStatus Status { get; set; } = AlertStatus.New;
    public DateTime CreatedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

