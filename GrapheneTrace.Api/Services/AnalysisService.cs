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
        var patient = await _dbContext.Users
            .Include(u => u.AssignedClinician)
            .FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken)
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

        // Get assigned clinician name
        var assignedClinicianName = patient.AssignedClinician?.FullName;

        // Get all comments (both patient and clinician)
        var allComments = await _dbContext.Comments
            .Include(c => c.Author)
            .Where(c => c.PatientId == patientId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new PatientMessageDto
            {
                MessageId = c.Id,
                AuthorName = c.Author.FullName,
                Message = c.Text,
                Timestamp = c.CreatedAt,
                IsClinician = c.Author.Role == UserRole.Clinician
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            CurrentRiskIndicator = latestData?.RiskLevel ?? RiskLevel.Low,
            LatestPeakPressureIndex = latestData?.PeakPressureIndex ?? 0,
            LatestContactAreaPercent = latestData?.ContactAreaPercent ?? 0,
            LastFrameTimestamp = latestData?.Timestamp,
            AlertSummary = alertSummary,
            LatestClinicianReply = latestClinicianReply,
            RecentAlerts = recentAlerts,
            AssignedClinicianName = assignedClinicianName,
            AllComments = allComments
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

    public async Task<PatientProfileDto> GetProfileAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users
            .Include(u => u.AssignedClinician)
            .FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        return new PatientProfileDto
        {
            FullName = patient.FullName,
            Email = patient.Email,
            DateOfBirth = patient.DateOfBirth,
            PhoneNumber = patient.PhoneNumber,
            Address = patient.Address,
            CreatedAt = patient.CreatedAt,
            LastLoginAt = patient.LastLoginAt,
            AssignedClinician = patient.AssignedClinician != null
                ? new AssignedClinicianDto
                {
                    FullName = patient.AssignedClinician.FullName,
                    Email = patient.AssignedClinician.Email,
                    PhoneNumber = patient.AssignedClinician.PhoneNumber,
                    Specialization = null, // Not stored in User model currently
                    ClinicName = null // Not stored in User model currently
                }
                : null
        };
    }

    public async Task<PatientProfileDto> UpdateProfileAsync(Guid patientId, UpdatePatientProfileDto updateDto, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users
            .Include(u => u.AssignedClinician)
            .FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        // Update patient properties
        patient.FullName = updateDto.FullName;
        patient.DateOfBirth = updateDto.DateOfBirth;
        patient.PhoneNumber = updateDto.PhoneNumber;
        patient.Address = updateDto.Address;
        patient.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PatientProfileDto
        {
            FullName = patient.FullName,
            Email = patient.Email,
            DateOfBirth = patient.DateOfBirth,
            PhoneNumber = patient.PhoneNumber,
            Address = patient.Address,
            CreatedAt = patient.CreatedAt,
            LastLoginAt = patient.LastLoginAt,
            AssignedClinician = patient.AssignedClinician != null
                ? new AssignedClinicianDto
                {
                    FullName = patient.AssignedClinician.FullName,
                    Email = patient.AssignedClinician.Email,
                    PhoneNumber = patient.AssignedClinician.PhoneNumber,
                    Specialization = null,
                    ClinicName = null
                }
                : null
        };
    }

    public async Task<bool> ChangePasswordAsync(Guid patientId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken = default)
    {
        var patient = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken)
            ?? throw new KeyNotFoundException("Patient not found.");

        // Verify current password
        if (!SecurityUtils.VerifyPassword(changePasswordDto.CurrentPassword, patient.PasswordHash))
        {
            return false;
        }

        // Update password
        patient.PasswordHash = SecurityUtils.HashPassword(changePasswordDto.NewPassword);
        patient.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<byte[]?> GenerateReportAsync(Guid patientId, string reportType, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == patientId, cancellationToken);

            if (patient == null)
            {
                _logger.LogWarning("Patient not found: {PatientId}", patientId);
                return null;
            }

            // Get patient data for the report
            var now = DateTime.UtcNow;
            DateTime? fromDate = reportType.ToLowerInvariant() switch
            {
                "daily" => now.AddDays(-1),
                "weekly" => now.AddDays(-7),
                "clinician-summary" => null, // All data
                _ => now.AddDays(-1)
            };

            var query = _dbContext.PatientData
                .Where(pd => pd.PatientId == patientId);

            if (fromDate.HasValue)
            {
                query = query.Where(pd => pd.Timestamp >= fromDate.Value);
            }

            var dataPoints = await query
                .OrderBy(pd => pd.Timestamp)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Generating {ReportType} report for patient {PatientId}, found {Count} data points", 
                reportType, patientId, dataPoints.Count);

            // Generate a simple PDF report
            // For now, we'll create a basic text-based PDF
            // In production, you might want to use a library like QuestPDF, iTextSharp, or PdfSharp
            var reportContent = GenerateReportContent(patient, dataPoints, reportType);
            
            if (string.IsNullOrWhiteSpace(reportContent))
            {
                _logger.LogWarning("Generated empty report content for patient {PatientId}", patientId);
                reportContent = $"Report: {reportType.ToUpperInvariant()}\nPatient: {patient.FullName}\nGenerated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nNo data available for the selected period.";
            }
            
            var pdfBytes = GenerateSimplePdf(reportContent);
            
            _logger.LogInformation("Generated PDF report, size: {Size} bytes", pdfBytes?.Length ?? 0);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report for patient {PatientId}, type {ReportType}", patientId, reportType);
            return null;
        }
    }

    private static string GenerateReportContent(Core.Models.User patient, List<PatientData> dataPoints, string reportType)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine($"  {reportType.ToUpperInvariant()} REPORT");
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine();
        sb.AppendLine($"Patient Name: {patient.FullName}");
        sb.AppendLine($"Email: {patient.Email ?? "N/A"}");
        sb.AppendLine($"Report Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Report Type: {reportType}");
        sb.AppendLine();
        sb.AppendLine("-".PadRight(60, '-'));
        sb.AppendLine($"Total Data Points: {dataPoints.Count}");
        sb.AppendLine("-".PadRight(60, '-'));

        if (dataPoints.Count > 0)
        {
            var avgPeakPressure = dataPoints.Average(p => p.PeakPressureIndex);
            var avgContactArea = dataPoints.Average(p => p.ContactAreaPercent);
            var minPeakPressure = dataPoints.Min(p => p.PeakPressureIndex);
            var maxPeakPressure = dataPoints.Max(p => p.PeakPressureIndex);
            var highRiskCount = dataPoints.Count(p => p.RiskLevel == RiskLevel.High || p.RiskLevel == RiskLevel.Critical);
            var mediumRiskCount = dataPoints.Count(p => p.RiskLevel == RiskLevel.Medium);
            var lowRiskCount = dataPoints.Count(p => p.RiskLevel == RiskLevel.Low);

            sb.AppendLine();
            sb.AppendLine("SUMMARY STATISTICS");
            sb.AppendLine("-".PadRight(60, '-'));
            sb.AppendLine($"  Average Peak Pressure Index: {avgPeakPressure:F2}");
            sb.AppendLine($"  Minimum Peak Pressure Index: {minPeakPressure:F2}");
            sb.AppendLine($"  Maximum Peak Pressure Index: {maxPeakPressure:F2}");
            sb.AppendLine($"  Average Contact Area: {avgContactArea:F2}%");
            sb.AppendLine();
            sb.AppendLine("RISK LEVEL DISTRIBUTION");
            sb.AppendLine("-".PadRight(60, '-'));
            sb.AppendLine($"  Low Risk Events: {lowRiskCount}");
            sb.AppendLine($"  Medium Risk Events: {mediumRiskCount}");
            sb.AppendLine($"  High/Critical Risk Events: {highRiskCount}");
            sb.AppendLine();
            sb.AppendLine("RECENT DATA POINTS (Last 20)");
            sb.AppendLine("-".PadRight(60, '-'));
            sb.AppendLine("  Timestamp                | PPI    | Contact % | Risk Level");
            sb.AppendLine("  ".PadRight(60, '-'));
            
            foreach (var point in dataPoints.OrderByDescending(p => p.Timestamp).Take(20))
            {
                var timestamp = point.Timestamp.ToString("yyyy-MM-dd HH:mm");
                var ppi = point.PeakPressureIndex.ToString("F2").PadLeft(6);
                var contact = point.ContactAreaPercent.ToString("F2").PadLeft(9);
                var risk = point.RiskLevel.ToString().PadRight(10);
                sb.AppendLine($"  {timestamp} | {ppi} | {contact}% | {risk}");
            }
            
            if (dataPoints.Count > 20)
            {
                sb.AppendLine();
                sb.AppendLine($"  ... and {dataPoints.Count - 20} more data points");
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("NO DATA AVAILABLE");
            sb.AppendLine("-".PadRight(60, '-'));
            sb.AppendLine("  No pressure data was found for the selected period.");
            sb.AppendLine("  This could mean:");
            sb.AppendLine("    - No data has been uploaded yet");
            sb.AppendLine("    - Data exists outside the selected time range");
            sb.AppendLine("    - Data collection has not started");
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine("End of Report");
        sb.AppendLine("=".PadRight(60, '='));

        return sb.ToString();
    }

    private static byte[] GenerateSimplePdf(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            content = "Empty Report\nNo content available.";
        }

        // Split content into lines, preserving empty lines for spacing
        var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        
        // Build content stream with proper PDF text positioning
        var contentStream = new System.Text.StringBuilder();
        contentStream.Append("BT\n");
        contentStream.Append("/F1 12 Tf\n");
        
        int yPos = 750;
        bool firstLine = true;
        
        foreach (var line in lines)
        {
            if (yPos < 50)
            {
                // Start new page if needed
                contentStream.Append("ET\n");
                contentStream.Append("BT\n");
                contentStream.Append("/F1 12 Tf\n");
                yPos = 750;
            }
            
            var escapedLine = EscapePdfString(line);
            
            if (firstLine)
            {
                contentStream.Append($"72 {yPos} Td\n");
                firstLine = false;
            }
            else
            {
                contentStream.Append($"0 -15 Td\n");
            }
            
            contentStream.Append($"({escapedLine}) Tj\n");
            yPos -= 15;
        }
        contentStream.Append("ET\n");
        
        var streamContent = contentStream.ToString();
        var streamBytes = System.Text.Encoding.ASCII.GetBytes(streamContent);
        
        // Build PDF structure
        var pdfParts = new List<(byte[] data, int length)>();
        
        // Header
        var header = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n");
        pdfParts.Add((header, header.Length));
        
        // Object 1: Catalog
        var catalog = System.Text.Encoding.ASCII.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdfParts.Add((catalog, catalog.Length));
        
        // Object 2: Pages
        var pages = System.Text.Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdfParts.Add((pages, pages.Length));
        
        // Object 3: Page
        var page = System.Text.Encoding.ASCII.GetBytes("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>\nendobj\n");
        pdfParts.Add((page, page.Length));
        
        // Object 4: Content stream
        var streamHeader = System.Text.Encoding.ASCII.GetBytes($"4 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        pdfParts.Add((streamHeader, streamHeader.Length));
        pdfParts.Add((streamBytes, streamBytes.Length));
        var streamFooter = System.Text.Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
        pdfParts.Add((streamFooter, streamFooter.Length));
        
        // Calculate offsets for xref
        int currentOffset = 0;
        var offsets = new List<int>();
        foreach (var (data, length) in pdfParts)
        {
            offsets.Add(currentOffset);
            currentOffset += length;
        }
        
        // XRef table
        var xref = new System.Text.StringBuilder();
        xref.Append("xref\n");
        xref.Append($"0 {pdfParts.Count + 1}\n");
        xref.Append("0000000000 65535 f \n");
        
        foreach (var offset in offsets)
        {
            xref.Append($"{offset:D10} 00000 n \n");
        }
        
        var xrefBytes = System.Text.Encoding.ASCII.GetBytes(xref.ToString());
        pdfParts.Add((xrefBytes, xrefBytes.Length));
        
        // Trailer
        var trailerOffset = currentOffset;
        var trailer = System.Text.Encoding.ASCII.GetBytes($"trailer\n<< /Size {pdfParts.Count} /Root 1 0 R >>\nstartxref\n{trailerOffset}\n%%EOF\n");
        pdfParts.Add((trailer, trailer.Length));
        
        // Combine all parts
        var totalLength = pdfParts.Sum(p => p.length);
        var result = new byte[totalLength];
        int position = 0;
        foreach (var (data, length) in pdfParts)
        {
            Buffer.BlockCopy(data, 0, result, position, length);
            position += length;
        }
        
        return result;
    }

    private static string EscapePdfString(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("\\", "\\\\")
                   .Replace("(", "\\(")
                   .Replace(")", "\\)")
                   .Replace("\r", "");
    }
}

