namespace GrapheneTrace.Core.DTOs.Admin;

public class DashboardKpiDto
{
    public int TotalUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalClinicians { get; set; }
    public int ActiveAlerts { get; set; }
    public DateTime? LastDataReceived { get; set; }
    public long TotalFramesStored { get; set; }
}

