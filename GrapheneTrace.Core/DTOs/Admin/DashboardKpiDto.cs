namespace GrapheneTrace.Core.DTOs.Admin;

public class DashboardKpiDto
{
    public int TotalUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalClinicians { get; set; }
    public int ActiveAlerts { get; set; }
    public int TotalAlerts { get; set; }
    public int NewAlerts { get; set; }
    public int InReviewAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int AutoClearedAlerts { get; set; }
    public DateTime? LastDataReceived { get; set; }
    public long TotalFramesStored { get; set; }
    public DashboardLabelsDto Labels { get; set; } = new();
}

public class DashboardLabelsDto
{
    public string PageTitle { get; set; } = string.Empty;
    public string PageSubtitle { get; set; } = string.Empty;
    public string ManageAdminsButton { get; set; } = string.Empty;
    
    // KPI Card Labels
    public string ActivePatientsTitle { get; set; } = string.Empty;
    public string ActivePatientsDescription { get; set; } = string.Empty;
    public string CliniciansTitle { get; set; } = string.Empty;
    public string CliniciansDescription { get; set; } = string.Empty;
    public string DataVolumeTitle { get; set; } = string.Empty;
    public string DataVolumeDescription { get; set; } = string.Empty;
    public string ActiveAlertsTitle { get; set; } = string.Empty;
    public string ActiveAlertsDescription { get; set; } = string.Empty;
    
    // System Overview Labels
    public string SystemOverviewTitle { get; set; } = string.Empty;
    public string SystemOverviewSubtitle { get; set; } = string.Empty;
    public string TotalUsersLabel { get; set; } = string.Empty;
    public string LastDataReceivedLabel { get; set; } = string.Empty;
    
    // System Status Labels
    public string SystemStatusTitle { get; set; } = string.Empty;
    public string SystemStatusSubtitle { get; set; } = string.Empty;
    public string TotalUsersStatusLabel { get; set; } = string.Empty;
    public string ActiveAlertsStatusLabel { get; set; } = string.Empty;
    public string DataFramesStatusLabel { get; set; } = string.Empty;
    
    // Audit Activity Labels
    public string RecentAuditActivityTitle { get; set; } = string.Empty;
    public string RecentAuditActivitySubtitle { get; set; } = string.Empty;
    public string ViewAuditConsoleButton { get; set; } = string.Empty;
    public string NoAuditLogsMessage { get; set; } = string.Empty;
    
    // Table Headers
    public string TableHeaderWhen { get; set; } = string.Empty;
    public string TableHeaderUser { get; set; } = string.Empty;
    public string TableHeaderAction { get; set; } = string.Empty;
    public string TableHeaderTarget { get; set; } = string.Empty;
}

