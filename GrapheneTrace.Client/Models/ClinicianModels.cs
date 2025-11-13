using MudBlazor;

namespace GrapheneTrace.Client.Models;

public record ClinicianAlertCard(string Title, string Value, string Trend, Color TrendColor, string Icon, Color IconColor);

public record ClinicianRiskPatient(
    string PatientId,
    string Name,
    string RiskLevel,
    Color RiskColor,
    string LastAlertSummary,
    DateTime LastAlertAt,
    string SensorStatus);

public record ClinicianDirectoryPatient(
    string PatientId,
    string Name,
    string RiskLevel,
    int TotalAlerts,
    DateTime LastAlertAt,
    string LastAlertSeverity,
    string AssignedDevice);

public record ClinicianAlertEvent(
    Guid Id,
    string PatientId,
    string PatientName,
    DateTime Timestamp,
    string EventType,
    string Status,
    string Severity,
    string Summary);

public record ClinicianMessageThread(
    Guid Id,
    string PatientId,
    string PatientName,
    DateTime StartedAt,
    string Summary,
    IReadOnlyList<ClinicianMessage> Messages);

public record ClinicianMessage(
    string Author,
    DateTime Timestamp,
    string Body,
    bool IsClinician);

public record ClinicianReportMetric(
    string Metric,
    string CurrentValue,
    string ComparisonValue,
    string DeltaText,
    Color DeltaColor,
    string TrendIcon);

public record HeatMapFrame(DateTime Timestamp, IReadOnlyList<IReadOnlyList<int>> Values);

public record TimelineMarker(DateTime Timestamp, string Label, Color Color);

