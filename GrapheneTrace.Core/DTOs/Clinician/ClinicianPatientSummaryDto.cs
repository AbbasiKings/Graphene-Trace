using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class ClinicianPatientSummaryDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public int ActiveAlerts { get; set; }
    public DateTime? LastAlertAt { get; set; }
    public string? LastAlertReason { get; set; }
}

