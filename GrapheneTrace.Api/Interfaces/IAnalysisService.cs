using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Models;

namespace GrapheneTrace.Api.Interfaces;

public interface IAnalysisService
{
    Task<PatientData> ProcessAndSaveFrameAsync(Guid patientId, DataUploadDto dataUploadDto, CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrendDataDto>> GetTrendDataAsync(Guid patientId, DateTime? fromUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientAlertDto>> GetAlertsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Comment> CreateQuickLogAsync(Guid patientId, Guid authorId, QuickLogDto request, CancellationToken cancellationToken = default);
    Task<FileUploadResultDto> ProcessUploadedFileAsync(Guid patientId, string fileName, Stream stream, CancellationToken cancellationToken = default);
}

