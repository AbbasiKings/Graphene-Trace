using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Models;

namespace GrapheneTrace.Api.Interfaces;

public interface IAnalysisService
{
    Task<PatientData> ProcessFrameAsync(Guid patientId, string csvData, CancellationToken cancellationToken = default);
    Task<PatientDashboardDto> GetPatientDashboardAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Comment> CreateQuickLogAsync(Guid patientId, Guid authorId, QuickLogDto request, CancellationToken cancellationToken = default);
}

