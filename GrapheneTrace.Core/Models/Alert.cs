using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.Models;

public class Alert : BaseEntity
{
    public Guid PatientDataId { get; set; }
    public PatientData PatientData { get; set; } = default!;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public AlertStatus Status { get; set; } = AlertStatus.New;
    public string Reason { get; set; } = string.Empty;
    public string? ClinicianNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

