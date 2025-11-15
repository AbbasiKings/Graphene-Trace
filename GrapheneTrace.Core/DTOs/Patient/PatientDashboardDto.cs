using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Patient;

public class PatientDashboardDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public double PeakPressureIndex { get; set; }
    public double ContactAreaPercent { get; set; }
    public RiskLevel CurrentRiskLevel { get; set; } = RiskLevel.Low;
    public DateTime? LastFrameTimestamp { get; set; }
    public IReadOnlyList<PatientAlertDto> ActiveAlerts { get; set; } = Array.Empty<PatientAlertDto>();
}

