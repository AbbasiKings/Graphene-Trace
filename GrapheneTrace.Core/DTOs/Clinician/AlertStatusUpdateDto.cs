using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class AlertStatusUpdateDto
{
    public AlertStatus NewStatus { get; set; } = AlertStatus.InReview;
    public string? ClinicianNotes { get; set; }
}

