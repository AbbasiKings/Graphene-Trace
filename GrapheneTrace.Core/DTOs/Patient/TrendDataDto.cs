using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Patient;

public class TrendDataDto
{
    public Guid PatientDataId { get; set; }
    public DateTime Timestamp { get; set; }
    public double PeakPressureIndex { get; set; }
    public double ContactAreaPercent { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
}

