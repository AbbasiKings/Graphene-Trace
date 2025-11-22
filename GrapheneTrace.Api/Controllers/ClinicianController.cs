using System.Security.Claims;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Clinician;
using GrapheneTrace.Core.DTOs.Patient;
using GrapheneTrace.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Clinician))]
public class ClinicianController(IClinicianService clinicianService) : ControllerBase
{
    private readonly IClinicianService _clinicianService = clinicianService;

    [HttpGet("triage")]
    public async Task<IActionResult> GetTriage(CancellationToken cancellationToken)
    {
        var clinicianId = GetUserId();
        var data = await _clinicianService.GetTriageListAsync(clinicianId, cancellationToken);
        return Ok(data);
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientDetails(Guid patientId, CancellationToken cancellationToken)
    {
        var clinicianId = GetUserId();
        var details = await _clinicianService.GetPatientDetailsAsync(patientId, clinicianId, cancellationToken);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("data/{dataId:guid}/raw")]
    public async Task<IActionResult> GetRawData(Guid dataId, CancellationToken cancellationToken)
    {
        var data = await _clinicianService.GetRawDataAsync(dataId, cancellationToken);
        return data is null ? NotFound() : Ok(data);
    }

    [HttpPut("alerts/{alertId:guid}/status")]
    public async Task<IActionResult> UpdateAlert(Guid alertId, AlertStatusUpdateDto request, CancellationToken cancellationToken)
    {
        var clinicianId = GetUserId();
        var success = await _clinicianService.UpdateAlertStatusAsync(alertId, request, clinicianId, cancellationToken);
        return success ? Ok() : Forbid();
    }

    [HttpPost("patient/{patientId:guid}/reply")]
    public async Task<IActionResult> Reply(Guid patientId, QuickLogDto request, CancellationToken cancellationToken)
    {
        var clinicianId = GetUserId();
        var success = await _clinicianService.ReplyToPatientAsync(patientId, clinicianId, request, cancellationToken);
        return success ? Ok() : Forbid();
    }

    [HttpGet("reports/{patientId:guid}")]
    public async Task<IActionResult> DownloadReport(Guid patientId, [FromQuery] string? startDate, [FromQuery] string? endDate, CancellationToken cancellationToken)
    {
        var clinicianId = GetUserId();
        
        DateTime? start = null;
        DateTime? end = null;
        
        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
        {
            start = parsedStart;
        }
        
        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
        {
            end = parsedEnd;
        }

        var reportBytes = await _clinicianService.GenerateReportAsync(patientId, clinicianId, start, end, cancellationToken);

        if (reportBytes == null || reportBytes.Length == 0)
        {
            return NotFound("Report not found or could not be generated.");
        }

        var fileName = $"Patient_Report_{patientId.ToString("N")[..8]}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        return File(reportBytes, "application/pdf", fileName);
    }

    [HttpGet("reports/{patientId:guid}/csv")]
    public async Task<IActionResult> DownloadReportCsv(Guid patientId, [FromQuery] string? startDate, [FromQuery] string? endDate, CancellationToken cancellationToken)
    {
        var clinicianId = GetUserId();
        
        DateTime? start = null;
        DateTime? end = null;
        
        if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStart))
        {
            start = parsedStart;
        }
        
        if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEnd))
        {
            end = parsedEnd;
        }

        var csvContent = await _clinicianService.GenerateReportCsvAsync(patientId, clinicianId, start, end, cancellationToken);

        if (csvContent == null || csvContent.Length == 0)
        {
            return NotFound("Report not found or could not be generated.");
        }

        var fileName = $"Patient_Report_{patientId.ToString("N")[..8]}_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(csvContent, "text/csv", fileName);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

