using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class TriagePatientDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public RiskLevel HighestRisk { get; set; } = RiskLevel.Low;
    public int NewAlertCount { get; set; }
    public int NewCommentCount { get; set; }
    public DateTime? LastDataReceived { get; set; }
}

