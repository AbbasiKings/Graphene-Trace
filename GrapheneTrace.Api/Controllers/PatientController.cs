using System.Security.Claims;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Patient))]
public class PatientController(IAnalysisService analysisService) : ControllerBase
{
    private readonly IAnalysisService _analysisService = analysisService;

    [HttpPost("data")]
    public async Task<IActionResult> UploadData(DataUploadDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawDataString))
        {
            return BadRequest("RawDataString is required.");
        }

        var patientId = GetUserId();
        var data = await _analysisService.ProcessAndSaveFrameAsync(patientId, request, cancellationToken);
        return Ok(new { data.Id, data.PeakPressureIndex, data.ContactAreaPercent, data.RiskLevel });
    }

    [HttpPost("data/files")]
    [RequestSizeLimit(50_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<IEnumerable<FileUploadResultDto>>> UploadFiles([FromForm] List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest("Please select at least one CSV file.");
        }

        var patientId = GetUserId();
        var results = new List<FileUploadResultDto>();

        foreach (var file in files)
        {
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new FileUploadResultDto
                {
                    FileName = file.FileName,
                    Status = "Skipped",
                    Errors = new[] { "Only CSV files are supported." }
                });
                continue;
            }

            await using var stream = file.OpenReadStream();
            var uploadResult = await _analysisService.ProcessUploadedFileAsync(patientId, file.FileName, stream, cancellationToken);
            results.Add(uploadResult);
        }

        return Ok(results);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var dashboard = await _analysisService.GetDashboardSummaryAsync(patientId, cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("trends")]
    public async Task<ActionResult<IEnumerable<TrendDataDto>>> GetTrends([FromQuery] string? timePeriod, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var fromUtc = ResolveTrendWindow(timePeriod);
        var data = await _analysisService.GetTrendDataAsync(patientId, fromUtc, cancellationToken);
        return Ok(data);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<PatientAlertDto>>> GetAlerts(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var alerts = await _analysisService.GetAlertsAsync(patientId, cancellationToken);
        return Ok(alerts);
    }

    [HttpPost("quicklog")]
    public async Task<IActionResult> QuickLog(QuickLogDto request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var entry = await _analysisService.CreateQuickLogAsync(patientId, patientId, request, cancellationToken);
        return Ok(new { entry.Id, entry.Text, entry.CreatedAt });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static DateTime? ResolveTrendWindow(string? timePeriod)
    {
        if (string.IsNullOrWhiteSpace(timePeriod))
        {
            return null;
        }

        return timePeriod.ToLowerInvariant() switch
        {
            "last-6h" => DateTime.UtcNow.AddHours(-6),
            "last-24h" => DateTime.UtcNow.AddHours(-24),
            "last-7d" => DateTime.UtcNow.AddDays(-7),
            _ => null
        };
    }
}

