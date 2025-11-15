using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Patient;

public class PatientAlertDto
{
    public Guid AlertId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public AlertStatus Status { get; set; } = AlertStatus.New;
    public DateTime CreatedAt { get; set; }
}

