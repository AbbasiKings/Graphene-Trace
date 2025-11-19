using System.Security.Claims;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Admin;
using GrapheneTrace.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService, IAuthService authService, IAuditService auditService) : ControllerBase
{
    private readonly IAdminService _adminService = adminService;
    private readonly IAuthService _authService = authService;
    private readonly IAuditService _auditService = auditService;

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardKpiDto>> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _adminService.GetDashboardKpisAsync(cancellationToken));

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        => Ok(await _adminService.GetAllUsersAsync(cancellationToken));

    [HttpPost]
    [Route("users")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] NewUserDto request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest("Full Name is required.");
        }

        var result = await _authService.CreateUserAsync(request, cancellationToken);
        if (result is null)
        {
            return Conflict("Email already exists.");
        }

        // Log the action
        var adminUserId = GetUserId();
        await _auditService.LogActionAsync(
            adminUserId,
            "UserCreated",
            "User",
            result.Id,
            $"{{\"email\":\"{request.Email}\",\"role\":\"{request.Role}\"}}",
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("users/{userId}")]
    public async Task<IActionResult> UpdateUser(Guid userId, UpdateUserDto request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateUserAsync(userId, request, cancellationToken);
        if (result is null)
        {
            return NotFound("User not found or invalid data provided.");
        }

        // Log the action
        var adminUserId = GetUserId();
        await _auditService.LogActionAsync(
            adminUserId,
            "UserUpdated",
            "User",
            userId,
            $"{{\"email\":\"{request.Email}\",\"role\":\"{request.Role}\",\"isActive\":{request.IsActive.ToString().ToLower()}}}",
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _authService.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var wasActive = user.IsActive;
        var userEmail = user.Email;

        var result = await _adminService.DeleteUserAsync(userId, cancellationToken);
        if (!result)
        {
            return BadRequest("Unable to delete user.");
        }

        // Log the action
        var adminUserId = GetUserId();
        var actionName = wasActive ? "UserDeactivated" : "UserDeleted";
        await _auditService.LogActionAsync(
            adminUserId,
            actionName,
            "User",
            userId,
            $"{{\"email\":\"{userEmail}\"}}",
            cancellationToken);

        return Ok(new { message = "User deleted successfully." });
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetSystemAudit([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
        => Ok(await _adminService.GetSystemAuditLogsAsync(limit, cancellationToken));

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("userId")?.Value
            ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}

