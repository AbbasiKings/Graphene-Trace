using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class RawPatientDataDto
{
    public Guid DataId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string RawCsvData { get; set; } = string.Empty;
    public double PeakPressureIndex { get; set; }
    public double ContactAreaPercent { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
}

