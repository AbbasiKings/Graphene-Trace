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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        var dashboard = await _analysisService.GetPatientDashboardAsync(patientId, cancellationToken);
        return Ok(dashboard);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFrame(UploadFrameDto request, CancellationToken cancellationToken)
    {
        var patientId = GetUserId();
        if (request.PatientId != Guid.Empty && request.PatientId != patientId)
        {
            return Forbid();
        }

        var data = await _analysisService.ProcessFrameAsync(patientId, request.CsvData, cancellationToken);
        return Ok(new { data.Id, data.PeakPressureIndex, data.ContactAreaPercent, data.RiskLevel });
    }

    [HttpPost("quick-log")]
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
}

