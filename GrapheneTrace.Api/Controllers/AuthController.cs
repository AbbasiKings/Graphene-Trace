using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        if (!response.Status)
        {
            return Unauthorized(response);
        }

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto request, CancellationToken cancellationToken)
    {
        var success = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return success ? Ok() : NotFound();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(PasswordResetDto request, CancellationToken cancellationToken)
    {
        var success = await _authService.ResetPasswordAsync(request, cancellationToken);
        return success ? Ok() : BadRequest();
    }
}

