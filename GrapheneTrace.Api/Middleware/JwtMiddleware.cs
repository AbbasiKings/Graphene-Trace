using System.Security.Claims;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.Constants;
using GrapheneTrace.Core.Utils;

namespace GrapheneTrace.Api.Middleware;

public class JwtMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JwtMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (!string.IsNullOrWhiteSpace(token))
        {
            await AttachUserToContextAsync(context, token, authService);
        }

        await _next(context);
    }

    private async Task AttachUserToContextAsync(HttpContext context, string token, IAuthService authService)
    {
        try
        {
            var validationResult = SecurityUtils.ValidateToken(token, GetSecret());
            if (!validationResult.IsValid)
            {
                return;
            }

            var user = await authService.GetUserAsync(validationResult.UserId, context.RequestAborted);
            if (user is null || !user.IsActive)
            {
                return;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Role, user.Role.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, "GrapheneJwt");
            context.User = new ClaimsPrincipal(identity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
        }
    }

    private string GetSecret()
        => Environment.GetEnvironmentVariable("JWT_SECRET")
           ?? _configuration["Jwt:Secret"]
           ?? AppConstants.DefaultJwtSecret;
}

