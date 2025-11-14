using MudBlazor;

namespace GrapheneTrace.Client.Models;

public record PatientRiskCard(string Title, string Value, string Context, Color CardColor, string Icon);

public record PatientMetricSnapshot(string Label, string Value, string TrendLabel, Color TrendColor, string Icon);

public record PatientAlertDigest(Guid Id, DateTime Timestamp, string Title, string Region, string Status, Color StatusColor);

public record PatientMessage(Guid Id, string Author, DateTime Timestamp, string Body, bool IsClinician);

public record PatientMessageThread(Guid Id, string ClinicianName, IReadOnlyList<PatientMessage> Messages);

public record PatientMetricTrend(string[] Labels, IReadOnlyList<ChartSeries> Series);

public record PatientReportSummary(string Title, DateTime GeneratedAt, string Description, string DownloadLabel, string Icon);

public record PatientNotificationPreference(string Label, string Description)
{
    public bool Enabled { get; set; }
}

public record PatientProfileSection(string Header, string Subtitle, string Icon);

