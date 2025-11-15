using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Clinician;
using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Services;

public class ClinicianService(AppDbContext dbContext) : IClinicianService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<ClinicianPatientSummaryDto>> GetTriageAsync(Guid clinicianId, CancellationToken cancellationToken = default)
    {
        var patients = await _dbContext.Users
            .Where(u => u.Role == UserRole.Patient && u.AssignedClinicianId == clinicianId)
            .Include(u => u.PatientData)
            .ThenInclude(pd => pd.Alerts)
            .ToListAsync(cancellationToken);

        return patients.Select(p =>
        {
            var lastAlert = p.PatientData
                .SelectMany(pd => pd.Alerts)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            var activeAlerts = p.PatientData.SelectMany(pd => pd.Alerts).Count(a => a.Status != AlertStatus.Resolved);

            return new ClinicianPatientSummaryDto
            {
                PatientId = p.Id,
                PatientName = p.FullName,
                RiskLevel = lastAlert?.RiskLevel ?? RiskLevel.Low,
                ActiveAlerts = activeAlerts,
                LastAlertAt = lastAlert?.CreatedAt,
                LastAlertReason = lastAlert?.Reason
            };
        }).ToList();
    }

    public async Task<RawPatientDataDto?> GetRawDataAsync(Guid dataId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PatientData
            .Include(pd => pd.Patient)
            .FirstOrDefaultAsync(pd => pd.Id == dataId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new RawPatientDataDto
        {
            DataId = entity.Id,
            PatientId = entity.PatientId,
            PatientName = entity.Patient.FullName,
            Timestamp = entity.Timestamp,
            RawCsvData = entity.RawCsvData,
            PeakPressureIndex = entity.PeakPressureIndex,
            ContactAreaPercent = entity.ContactAreaPercent,
            RiskLevel = entity.RiskLevel
        };
    }

    public async Task<bool> UpdateAlertStatusAsync(Guid alertId, AlertStatusUpdateDto request, Guid clinicianId, CancellationToken cancellationToken = default)
    {
        var alert = await _dbContext.Alerts
            .Include(a => a.PatientData)
            .ThenInclude(pd => pd.Patient)
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);

        if (alert is null)
        {
            return false;
        }

        if (alert.PatientData.Patient.AssignedClinicianId != clinicianId)
        {
            return false;
        }

        alert.Status = request.NewStatus;
        alert.ClinicianNotes = request.ClinicianNotes;
        alert.ResolvedAt = request.NewStatus == AlertStatus.Resolved ? DateTime.UtcNow : null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReplyToPatientAsync(Guid patientId, Guid clinicianId, QuickLogDto request, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken);
        if (patient is null || patient.AssignedClinicianId != clinicianId)
        {
            return false;
        }

        var comment = new Comment
        {
            PatientId = patientId,
            AuthorId = clinicianId,
            Text = request.Text
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

