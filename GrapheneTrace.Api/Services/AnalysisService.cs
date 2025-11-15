using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;
using GrapheneTrace.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Services;

public class AnalysisService(AppDbContext dbContext, ILogger<AnalysisService> logger) : IAnalysisService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<AnalysisService> _logger = logger;

    public async Task<PatientData> ProcessFrameAsync(Guid patientId, string csvData, CancellationToken cancellationToken = default)
    {
        var matrix = FileProcessor.ParseCsvMatrix(csvData);
        var peak = FileProcessor.CalculatePeakPressureIndex(matrix);
        var contact = FileProcessor.CalculateContactAreaPercent(matrix);
        var risk = FileProcessor.DetermineRiskLevel(peak);

        var entity = new PatientData
        {
            PatientId = patientId,
            Timestamp = DateTime.UtcNow,
            RawCsvData = csvData,
            PeakPressureIndex = peak,
            ContactAreaPercent = contact,
            RiskLevel = risk,
            IsFlaggedForReview = risk >= RiskLevel.High
        };

        _dbContext.PatientData.Add(entity);

        if (risk >= RiskLevel.High)
        {
            _dbContext.Alerts.Add(new Alert
            {
                PatientData = entity,
                RiskLevel = risk,
                Status = AlertStatus.New,
                Reason = $"Peak pressure index {peak} exceeded threshold."
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Processed frame for patient {PatientId} (Risk: {Risk})", patientId, risk);
        return entity;
    }

    public async Task<PatientDashboardDto> GetPatientDashboardAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken)
                      ?? throw new KeyNotFoundException("Patient not found.");

        var latestData = await _dbContext.PatientData
            .Where(pd => pd.PatientId == patientId)
            .OrderByDescending(pd => pd.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var alerts = await _dbContext.Alerts
            .Where(a => a.PatientData.PatientId == patientId && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new PatientAlertDto
            {
                AlertId = a.Id,
                Reason = a.Reason,
                RiskLevel = a.RiskLevel,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PatientDashboardDto
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            PeakPressureIndex = latestData?.PeakPressureIndex ?? 0,
            ContactAreaPercent = latestData?.ContactAreaPercent ?? 0,
            CurrentRiskLevel = latestData?.RiskLevel ?? RiskLevel.Low,
            LastFrameTimestamp = latestData?.Timestamp,
            ActiveAlerts = alerts
        };
    }

    public async Task<Comment> CreateQuickLogAsync(Guid patientId, Guid authorId, QuickLogDto request, CancellationToken cancellationToken = default)
    {
        var comment = new Comment
        {
            PatientId = patientId,
            AuthorId = authorId,
            Text = request.Text
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return comment;
    }
}

