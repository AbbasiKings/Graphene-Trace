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

    public async Task<IReadOnlyList<TriagePatientDto>> GetTriageListAsync(Guid clinicianId, CancellationToken cancellationToken = default)
    {
        var patients = await _dbContext.Users
            .Where(u => u.Role == UserRole.Patient && u.AssignedClinicianId == clinicianId)
            .Include(u => u.PatientData)
            .ThenInclude(pd => pd.Alerts)
            .ToListAsync(cancellationToken);

        var patientIds = patients.Select(p => p.Id).ToList();

        var patientComments = await _dbContext.Comments
            .Where(c => patientIds.Contains(c.PatientId))
            .Include(c => c.Author)
            .ToListAsync(cancellationToken);

        return patients.Select(p =>
        {
            var allAlerts = p.PatientData.SelectMany(pd => pd.Alerts).ToList();
            var newAlerts = allAlerts.Count(a => a.Status == AlertStatus.New);
            var highestRiskAlert = allAlerts.OrderByDescending(a => a.RiskLevel).FirstOrDefault();
            var highestRisk = highestRiskAlert?.RiskLevel ?? RiskLevel.Low;

            var newComments = patientComments
                .Where(c => c.PatientId == p.Id && c.Author.Role == UserRole.Patient)
                .Count(c => c.CreatedAt > DateTime.UtcNow.AddDays(-1)); // Comments from last 24h

            var lastDataReceived = p.PatientData
                .OrderByDescending(pd => pd.Timestamp)
                .Select(pd => (DateTime?)pd.Timestamp)
                .FirstOrDefault();

            return new TriagePatientDto
            {
                PatientId = p.Id,
                PatientName = p.FullName,
                HighestRisk = highestRisk,
                NewAlertCount = newAlerts,
                NewCommentCount = newComments,
                LastDataReceived = lastDataReceived
            };
        }).OrderByDescending(t => t.HighestRisk)
          .ThenByDescending(t => t.NewAlertCount)
          .ThenByDescending(t => t.LastDataReceived)
          .ToList();
    }

    public async Task<PatientDetailDto?> GetPatientDetailsAsync(Guid patientId, Guid clinicianId, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == patientId && u.Role == UserRole.Patient, cancellationToken);

        if (patient is null)
        {
            // Patient doesn't exist
            return null;
        }

        // Check if patient is assigned to this clinician
        if (patient.AssignedClinicianId == null)
        {
            // Patient exists but not assigned to any clinician
            return null;
        }

        if (patient.AssignedClinicianId != clinicianId)
        {
            // Patient is assigned to a different clinician
            return null;
        }

        var metricTrends = await _dbContext.PatientData
            .Where(pd => pd.PatientId == patientId)
            .OrderByDescending(pd => pd.Timestamp)
            .Take(100) // Last 100 data points
            .Select(pd => new TrendDataDto
            {
                PatientDataId = pd.Id,
                Timestamp = pd.Timestamp,
                PeakPressureIndex = pd.PeakPressureIndex,
                ContactAreaPercent = pd.ContactAreaPercent,
                RiskLevel = pd.RiskLevel
            })
            .ToListAsync(cancellationToken);

        var activeAlerts = await _dbContext.Alerts
            .Where(a => a.PatientId == patientId && a.Status != AlertStatus.Resolved)
            .Include(a => a.PatientData)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var communicationHistory = await _dbContext.Comments
            .Where(c => c.PatientId == patientId)
            .Include(c => c.Author)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var activeAlertDtos = activeAlerts.Select(a => new ActiveAlertDto
        {
            AlertId = a.Id,
            PatientDataId = a.PatientDataId,
            Reason = a.Reason,
            RiskLevel = a.RiskLevel,
            Status = a.Status,
            ClinicianNotes = a.ClinicianNotes,
            CreatedAt = a.CreatedAt
        }).ToList();

        var communicationDtos = communicationHistory.Select(c => new CommunicationEntryDto
        {
            CommentId = c.Id,
            PatientDataId = c.PatientDataId,
            CreatedAt = c.CreatedAt,
            Text = c.Text,
            AuthorName = c.Author?.FullName ?? "Unknown",
            AuthorRole = c.Author?.Role ?? UserRole.Patient
        }).ToList();

        return new PatientDetailDto
        {
            PatientId = patient.Id,
            Name = patient.FullName,
            Email = patient.Email,
            Phone = null, // Phone not in User model currently
            MetricTrends = metricTrends,
            ActiveAlerts = activeAlertDtos,
            CommunicationHistory = communicationDtos
        };
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
            Text = request.CommentText
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

