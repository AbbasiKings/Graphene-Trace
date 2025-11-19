using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Patient;

public class DashboardSummaryDto
{
    public RiskLevel CurrentRiskIndicator { get; set; } = RiskLevel.Low;
    public double LatestPeakPressureIndex { get; set; }
    public double LatestContactAreaPercent { get; set; }
    public DateTime? LastFrameTimestamp { get; set; }
    public AlertSummaryDto AlertSummary { get; set; } = new();
    public ClinicianReplyDto? LatestClinicianReply { get; set; }
    public IReadOnlyList<PatientAlertDto> RecentAlerts { get; set; } = Array.Empty<PatientAlertDto>();
}

public class AlertSummaryDto
{
    public int TotalAlerts { get; set; }
    public int HighRiskAlerts { get; set; }
    public int NewAlerts { get; set; }
}

public class ClinicianReplyDto
{
    public string AuthorName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

