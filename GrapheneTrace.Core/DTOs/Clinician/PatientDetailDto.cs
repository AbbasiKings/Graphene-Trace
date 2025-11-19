using GrapheneTrace.Core.DTOs.Patient;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class PatientDetailDto
{
    public Guid PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<TrendDataDto> MetricTrends { get; set; } = new();
    public List<ActiveAlertDto> ActiveAlerts { get; set; } = new();
    public List<CommunicationEntryDto> CommunicationHistory { get; set; } = new();
}

