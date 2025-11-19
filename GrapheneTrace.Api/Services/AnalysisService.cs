using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.Constants;
using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;
using GrapheneTrace.Core.Utils;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GrapheneTrace.Api.Services;

public class AnalysisService(AppDbContext dbContext, ILogger<AnalysisService> logger) : IAnalysisService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<AnalysisService> _logger = logger;

    public async Task<PatientData> ProcessAndSaveFrameAsync(Guid patientId, DataUploadDto dataUploadDto, CancellationToken cancellationToken = default)
    {
        var csvData = dataUploadDto.RawDataString;
        var matrix = FileProcessor.ParseCsvMatrix(csvData);
        var peak = FileProcessor.CalculatePeakPressureIndex(matrix);
        var contact = FileProcessor.CalculateContactAreaPercent(matrix);
        var risk = FileProcessor.DetermineRiskLevel(peak);
        var timestamp = dataUploadDto.TimestampUtc ?? DateTime.UtcNow;

        var entity = new PatientData
        {
            PatientId = patientId,
            Timestamp = timestamp,
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
                PatientId = patientId,
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

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken)
                      ?? throw new KeyNotFoundException("Patient not found.");

        var latestData = await _dbContext.PatientData
            .Where(pd => pd.PatientId == patientId)
            .OrderByDescending(pd => pd.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var recentAlerts = await _dbContext.Alerts
            .Where(a => a.PatientId == patientId && a.Status != AlertStatus.Resolved)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new PatientAlertDto
            {
                AlertId = a.Id,
                PatientDataId = a.PatientDataId,
                Reason = a.Reason,
                RiskLevel = a.RiskLevel,
                Status = a.Status,
                ClinicianNotes = a.ClinicianNotes,
                Timestamp = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var alertSummary = await _dbContext.Alerts
            .Where(a => a.PatientId == patientId)
            .GroupBy(_ => 1)
            .Select(g => new AlertSummaryDto
            {
                TotalAlerts = g.Count(),
                HighRiskAlerts = g.Count(a => a.RiskLevel >= RiskLevel.High && a.Status != AlertStatus.Resolved),
                NewAlerts = g.Count(a => a.Status == AlertStatus.New)
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new AlertSummaryDto();

        var latestClinicianReply = await _dbContext.Comments
            .Include(c => c.Author)
            .Where(c => c.PatientId == patientId && c.Author.Role == UserRole.Clinician)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClinicianReplyDto
            {
                AuthorName = c.Author.FullName,
                Message = c.Text,
                Timestamp = c.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            CurrentRiskIndicator = latestData?.RiskLevel ?? RiskLevel.Low,
            LatestPeakPressureIndex = latestData?.PeakPressureIndex ?? 0,
            LatestContactAreaPercent = latestData?.ContactAreaPercent ?? 0,
            LastFrameTimestamp = latestData?.Timestamp,
            AlertSummary = alertSummary,
            LatestClinicianReply = latestClinicianReply,
            RecentAlerts = recentAlerts
        };
    }

    public async Task<IReadOnlyList<TrendDataDto>> GetTrendDataAsync(Guid patientId, DateTime? fromUtc, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PatientData
            .Where(pd => pd.PatientId == patientId);

        if (fromUtc.HasValue)
        {
            query = query.Where(pd => pd.Timestamp >= fromUtc.Value);
        }

        return await query
            .OrderByDescending(pd => pd.Timestamp)
            .Take(500)
            .Select(pd => new TrendDataDto
            {
                PatientDataId = pd.Id,
                Timestamp = pd.Timestamp,
                PeakPressureIndex = pd.PeakPressureIndex,
                ContactAreaPercent = pd.ContactAreaPercent,
                RiskLevel = pd.RiskLevel
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientAlertDto>> GetAlertsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Alerts
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PatientAlertDto
            {
                AlertId = a.Id,
                PatientDataId = a.PatientDataId,
                Reason = a.Reason,
                RiskLevel = a.RiskLevel,
                Status = a.Status,
                ClinicianNotes = a.ClinicianNotes,
                Timestamp = a.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Comment> CreateQuickLogAsync(Guid patientId, Guid authorId, QuickLogDto request, CancellationToken cancellationToken = default)
    {
        var comment = new Comment
        {
            PatientId = patientId,
            AuthorId = authorId,
            Text = request.CommentText
        };

        _dbContext.Comments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return comment;
    }

    public async Task<FileUploadResultDto> ProcessUploadedFileAsync(Guid patientId, string fileName, Stream stream, CancellationToken cancellationToken = default)
    {
        var result = new FileUploadResultDto
        {
            FileName = fileName,
            Status = "Processing"
        };

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Status = "Failed";
            result.Errors = new[] { "File was empty." };
            return result;
        }

        var frames = SplitFrames(content);
        if (frames.Count == 0)
        {
            result.Status = "Failed";
            result.Errors = new[] { "Unable to detect any 32x32 frames in the file." };
            return result;
        }

        var errors = new List<string>();
        var processed = 0;
        var alerts = 0;
        var baseTimestamp = TryExtractTimestamp(fileName) ?? DateTime.UtcNow;

        for (var index = 0; index < frames.Count; index++)
        {
            var framePayload = frames[index];
            try
            {
                var dto = new DataUploadDto
                {
                    RawDataString = framePayload,
                    TimestampUtc = baseTimestamp.AddSeconds(index * 5)
                };
                var data = await ProcessAndSaveFrameAsync(patientId, dto, cancellationToken);
                processed++;
                if (data.IsFlaggedForReview)
                {
                    alerts++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process frame {FrameIndex} inside {FileName}", index + 1, fileName);
                errors.Add($"Frame {index + 1}: {ex.Message}");
            }
        }

        result.FramesProcessed = processed;
        result.AlertsRaised = alerts;
        result.Status = processed > 0
            ? errors.Count > 0 ? "CompletedWithErrors" : "Completed"
            : "Failed";
        result.UploadedAt = DateTime.UtcNow;
        result.Errors = errors;
        return result;
    }

    private static IReadOnlyList<string> SplitFrames(string content)
    {
        var frames = new List<string>();
        var buffer = new List<string>();
        var lines = content.Replace("\r\n", "\n").Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushBuffer();
                continue;
            }

            buffer.Add(line.Trim());

            if (buffer.Count == AppConstants.CsvMatrixSize)
            {
                FlushBuffer();
            }
        }

        FlushBuffer();

        return frames;

        void FlushBuffer()
        {
            if (buffer.Count == AppConstants.CsvMatrixSize)
            {
                frames.Add(string.Join(Environment.NewLine, buffer));
            }
            buffer.Clear();
        }
    }

    private static DateTime? TryExtractTimestamp(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(nameWithoutExtension))
        {
            return null;
        }

        var digitsOnly = Regex.Replace(nameWithoutExtension, @"\D", string.Empty);
        var candidates = new[]
        {
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMdd"
        };

        foreach (var format in candidates)
        {
            if (digitsOnly.Length >= format.Length &&
                DateTime.TryParseExact(digitsOnly.Substring(0, format.Length), format, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }
        }

        return null;
    }
}

