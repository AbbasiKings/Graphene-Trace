using GrapheneTrace.Core.DTOs.Clinician;
using GrapheneTrace.Core.DTOs.Patient;

namespace GrapheneTrace.Api.Interfaces;

public interface IClinicianService
{
    Task<IReadOnlyList<TriagePatientDto>> GetTriageListAsync(Guid clinicianId, CancellationToken cancellationToken = default);
    Task<PatientDetailDto?> GetPatientDetailsAsync(Guid patientId, Guid clinicianId, CancellationToken cancellationToken = default);
    Task<RawPatientDataDto?> GetRawDataAsync(Guid dataId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAlertStatusAsync(Guid alertId, AlertStatusUpdateDto request, Guid clinicianId, CancellationToken cancellationToken = default);
    Task<bool> ReplyToPatientAsync(Guid patientId, Guid clinicianId, QuickLogDto request, CancellationToken cancellationToken = default);
    Task<byte[]?> GenerateReportAsync(Guid patientId, Guid clinicianId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<byte[]?> GenerateReportCsvAsync(Guid patientId, Guid clinicianId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
}

