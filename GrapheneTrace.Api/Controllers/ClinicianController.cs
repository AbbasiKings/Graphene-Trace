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
        var data = await _clinicianService.GetTriageAsync(clinicianId, cancellationToken);
        return Ok(data);
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

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

