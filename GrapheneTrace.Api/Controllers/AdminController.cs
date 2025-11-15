using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Admin;
using GrapheneTrace.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(IAdminService adminService, IAuthService authService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService;
    private readonly IAuthService _authService = authService;

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardKpiDto>> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _adminService.GetDashboardKpisAsync(cancellationToken));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        => Ok(await _authService.GetUsersAsync(cancellationToken));

    [HttpPost("users/create")]
    public async Task<IActionResult> CreateUser(NewUserDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.CreateUserAsync(request, cancellationToken);
        return result is null ? Conflict("Email already exists.") : Ok(result);
    }
}

