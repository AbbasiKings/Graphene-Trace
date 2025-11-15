using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.Models;

public class PatientData : BaseEntity
{
    public Guid PatientId { get; set; }
    public User Patient { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RawCsvData { get; set; } = string.Empty;
    public double PeakPressureIndex { get; set; }
    public double ContactAreaPercent { get; set; }
    public bool IsFlaggedForReview { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

