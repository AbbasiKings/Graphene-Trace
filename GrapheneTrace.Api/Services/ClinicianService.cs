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
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return patients.Select(p =>
        {
            var allAlerts = p.PatientData.SelectMany(pd => pd.Alerts).ToList();
            var newAlerts = allAlerts.Count(a => a.Status == AlertStatus.New);
            var highestRiskAlert = allAlerts.OrderByDescending(a => a.RiskLevel).FirstOrDefault();
            var highestRisk = highestRiskAlert?.RiskLevel ?? RiskLevel.Low;

            // Count patient-authored comments from last 90 days that haven't been replied to
            // A comment is "new" if it's from a patient and there's no clinician reply after it
            var allCommentsForPatient = patientComments
                .Where(c => c.PatientId == p.Id && c.CreatedAt > DateTime.UtcNow.AddDays(-90))
                .OrderBy(c => c.CreatedAt)
                .ToList();
            
            // Count patient comments that don't have a clinician reply after them
            var newComments = 0;
            foreach (var comment in allCommentsForPatient)
            {
                // Only count if it's a patient comment
                if (comment.Author != null && comment.Author.Role == UserRole.Patient)
                {
                    // Check if there's a clinician reply after this patient comment
                    var hasClinicianReply = allCommentsForPatient
                        .Any(c => c.Author != null
                               && c.Author.Role == UserRole.Clinician 
                               && c.CreatedAt > comment.CreatedAt);
                    
                    if (!hasClinicianReply)
                    {
                        newComments++;
                    }
                }
            }

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

    public async Task<byte[]?> GenerateReportAsync(Guid patientId, Guid clinicianId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify patient is assigned to this clinician
            var patient = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == patientId && u.AssignedClinicianId == clinicianId, cancellationToken);

            if (patient == null)
            {
                return null;
            }

            // Get patient data for the report
            var query = _dbContext.PatientData
                .Where(pd => pd.PatientId == patientId);

            if (startDate.HasValue)
            {
                query = query.Where(pd => pd.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(pd => pd.Timestamp <= endDate.Value.AddDays(1)); // Include the end date
            }

            var dataPoints = await query
                .OrderBy(pd => pd.Timestamp)
                .ToListAsync(cancellationToken);

            // Get alerts and comments for the period
            var alertsQuery = _dbContext.Alerts
                .Where(a => a.PatientId == patientId);

            if (startDate.HasValue)
            {
                alertsQuery = alertsQuery.Where(a => a.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                alertsQuery = alertsQuery.Where(a => a.CreatedAt <= endDate.Value.AddDays(1));
            }

            var alerts = await alertsQuery.ToListAsync(cancellationToken);

            var commentsQuery = _dbContext.Comments
                .Where(c => c.PatientId == patientId);

            if (startDate.HasValue)
            {
                commentsQuery = commentsQuery.Where(c => c.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                commentsQuery = commentsQuery.Where(c => c.CreatedAt <= endDate.Value.AddDays(1));
            }

            var comments = await commentsQuery
                .Include(c => c.Author)
                .ToListAsync(cancellationToken);

            // Generate report content
            var reportContent = GenerateClinicianReportContent(patient, dataPoints, alerts, comments, startDate, endDate);
            
            if (string.IsNullOrWhiteSpace(reportContent))
            {
                reportContent = $"CLINICIAN PATIENT REPORT\n\nPatient: {patient.FullName}\nGenerated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nNo data available for the selected period.";
            }
            
            var pdfBytes = GenerateSimplePdf(reportContent);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<byte[]?> GenerateReportCsvAsync(Guid patientId, Guid clinicianId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify patient is assigned to this clinician
            var patient = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == patientId && u.AssignedClinicianId == clinicianId, cancellationToken);

            if (patient == null)
            {
                return null;
            }

            // Get patient data for the report
            var query = _dbContext.PatientData
                .Where(pd => pd.PatientId == patientId);

            if (startDate.HasValue)
            {
                query = query.Where(pd => pd.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(pd => pd.Timestamp <= endDate.Value.AddDays(1));
            }

            var dataPoints = await query
                .OrderBy(pd => pd.Timestamp)
                .ToListAsync(cancellationToken);

            // Generate CSV content
            var csvContent = GenerateClinicianReportCsv(patient, dataPoints, startDate, endDate);
            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);

            return csvBytes;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    private static string GenerateClinicianReportCsv(Core.Models.User patient, List<Core.Models.PatientData> dataPoints, DateTime? startDate, DateTime? endDate)
    {
        var sb = new System.Text.StringBuilder();
        
        // Header
        sb.AppendLine("Patient Report - CSV Export");
        sb.AppendLine($"Patient Name,{patient.FullName}");
        sb.AppendLine($"Email,{patient.Email ?? "N/A"}");
        sb.AppendLine($"Report Generated,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        if (startDate.HasValue && endDate.HasValue)
        {
            sb.AppendLine($"Report Period,{startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}");
        }
        else
        {
            sb.AppendLine("Report Period,All available data");
        }
        sb.AppendLine();
        
        // Summary
        if (dataPoints.Count > 0)
        {
            var avgPpi = dataPoints.Average(p => p.PeakPressureIndex);
            var avgContact = dataPoints.Average(p => p.ContactAreaPercent);
            var minPpi = dataPoints.Min(p => p.PeakPressureIndex);
            var maxPpi = dataPoints.Max(p => p.PeakPressureIndex);
            
            sb.AppendLine("Summary Statistics");
            sb.AppendLine($"Average Peak Pressure Index,{avgPpi:F2}");
            sb.AppendLine($"Minimum Peak Pressure Index,{minPpi:F2}");
            sb.AppendLine($"Maximum Peak Pressure Index,{maxPpi:F2}");
            sb.AppendLine($"Average Contact Area %,{avgContact:F2}");
            sb.AppendLine();
        }
        
        // Data points
        sb.AppendLine("Data Points");
        sb.AppendLine("Timestamp,Peak Pressure Index,Contact Area %,Risk Level");
        
        foreach (var point in dataPoints)
        {
            sb.AppendLine($"{point.Timestamp:yyyy-MM-dd HH:mm:ss},{point.PeakPressureIndex:F2},{point.ContactAreaPercent:F2},{point.RiskLevel}");
        }
        
        return sb.ToString();
    }

    private static string GenerateClinicianReportContent(Core.Models.User patient, List<Core.Models.PatientData> dataPoints, List<Core.Models.Alert> alerts, List<Core.Models.Comment> comments, DateTime? startDate, DateTime? endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine("  CLINICIAN PATIENT REPORT");
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine();
        sb.AppendLine($"Patient Name: {patient.FullName}");
        sb.AppendLine($"Email: {patient.Email ?? "N/A"}");
        sb.AppendLine($"Report Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        if (startDate.HasValue && endDate.HasValue)
        {
            sb.AppendLine($"Report Period: {startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}");
        }
        else
        {
            sb.AppendLine("Report Period: All available data");
        }
        sb.AppendLine();
        sb.AppendLine("-".PadRight(60, '-'));
        sb.AppendLine($"Total Data Points: {dataPoints.Count}");
        sb.AppendLine($"Total Alerts: {alerts.Count}");
        sb.AppendLine($"Total Comments: {comments.Count}");
        sb.AppendLine("-".PadRight(60, '-'));

        if (dataPoints.Count > 0)
        {
            var avgPeakPressure = dataPoints.Average(p => p.PeakPressureIndex);
            var avgContactArea = dataPoints.Average(p => p.ContactAreaPercent);
            var minPeakPressure = dataPoints.Min(p => p.PeakPressureIndex);
            var maxPeakPressure = dataPoints.Max(p => p.PeakPressureIndex);
            var highRiskCount = dataPoints.Count(p => p.RiskLevel == Core.Enums.RiskLevel.High || p.RiskLevel == Core.Enums.RiskLevel.Critical);

            sb.AppendLine();
            sb.AppendLine("SUMMARY STATISTICS");
            sb.AppendLine("-".PadRight(60, '-'));
            sb.AppendLine($"  Average Peak Pressure Index: {avgPeakPressure:F2}");
            sb.AppendLine($"  Minimum Peak Pressure Index: {minPeakPressure:F2}");
            sb.AppendLine($"  Maximum Peak Pressure Index: {maxPeakPressure:F2}");
            sb.AppendLine($"  Average Contact Area: {avgContactArea:F2}%");
            sb.AppendLine($"  High/Critical Risk Events: {highRiskCount}");
            sb.AppendLine();
        }

        if (alerts.Count > 0)
        {
            sb.AppendLine("ALERTS SUMMARY");
            sb.AppendLine("-".PadRight(60, '-'));
            foreach (var alert in alerts.OrderByDescending(a => a.CreatedAt).Take(10))
            {
                sb.AppendLine($"  {alert.CreatedAt:yyyy-MM-dd HH:mm} - {alert.RiskLevel} - {alert.Reason}");
            }
            if (alerts.Count > 10)
            {
                sb.AppendLine($"  ... and {alerts.Count - 10} more alerts");
            }
            sb.AppendLine();
        }

        if (comments.Count > 0)
        {
            sb.AppendLine("COMMUNICATION HISTORY");
            sb.AppendLine("-".PadRight(60, '-'));
            foreach (var comment in comments.OrderByDescending(c => c.CreatedAt).Take(10))
            {
                var authorName = comment.Author?.FullName ?? "Unknown";
                var authorRole = comment.Author?.Role.ToString() ?? "Unknown";
                sb.AppendLine($"  {comment.CreatedAt:yyyy-MM-dd HH:mm} - {authorName} ({authorRole})");
                sb.AppendLine($"    {comment.Text.Substring(0, Math.Min(60, comment.Text.Length))}...");
            }
            if (comments.Count > 10)
            {
                sb.AppendLine($"  ... and {comments.Count - 10} more comments");
            }
            sb.AppendLine();
        }

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

        var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        
        var contentStream = new System.Text.StringBuilder();
        contentStream.Append("BT\n");
        contentStream.Append("/F1 12 Tf\n");
        
        int yPos = 750;
        bool firstLine = true;
        
        foreach (var line in lines)
        {
            if (yPos < 50)
            {
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
        
        var pdfParts = new List<(byte[] data, int length)>();
        
        var header = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n");
        pdfParts.Add((header, header.Length));
        
        var catalog = System.Text.Encoding.ASCII.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdfParts.Add((catalog, catalog.Length));
        
        var pages = System.Text.Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdfParts.Add((pages, pages.Length));
        
        var page = System.Text.Encoding.ASCII.GetBytes("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>\nendobj\n");
        pdfParts.Add((page, page.Length));
        
        var streamHeader = System.Text.Encoding.ASCII.GetBytes($"4 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        pdfParts.Add((streamHeader, streamHeader.Length));
        pdfParts.Add((streamBytes, streamBytes.Length));
        var streamFooter = System.Text.Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
        pdfParts.Add((streamFooter, streamFooter.Length));
        
        int currentOffset = 0;
        var offsets = new List<int>();
        foreach (var (data, length) in pdfParts)
        {
            offsets.Add(currentOffset);
            currentOffset += length;
        }
        
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
        
        var trailerOffset = currentOffset;
        var trailer = System.Text.Encoding.ASCII.GetBytes($"trailer\n<< /Size {pdfParts.Count} /Root 1 0 R >>\nstartxref\n{trailerOffset}\n%%EOF\n");
        pdfParts.Add((trailer, trailer.Length));
        
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

