using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.Constants;
using GrapheneTrace.Core.DTOs.Admin;
using GrapheneTrace.Core.DTOs.Auth;
using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;
using GrapheneTrace.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Services;

public class AuthService(AppDbContext dbContext, ILogger<AuthService> logger, IConfiguration configuration) : IAuthService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

        if (user is null || !user.IsActive || !SecurityUtils.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new LoginResponseDto
            {
                Status = false,
                Message = "Invalid credentials."
            };
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = SecurityUtils.GenerateToken(
            user,
            GetJwtSecret(),
            GetJwtExpiryDays());

        return new LoginResponseDto
        {
            Status = true,
            Message = "Login successful.",
            Token = token,
            Role = user.Role,
            UserName = user.FullName,
            UserId = user.Id
        };
    }

    public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(2);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "PasswordResetRequested",
            MetadataJson = $"{{\"email\":\"{user.Email}\"}}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token generated for {Email}", user.Email);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(PasswordResetDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == request.Token &&
            u.PasswordResetTokenExpiresAt > DateTime.UtcNow, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.PasswordHash = SecurityUtils.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "PasswordReset",
            MetadataJson = $"{{\"email\":\"{user.Email}\"}}"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AdminUserDto?> CreateUserAsync(NewUserDto request, CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            PasswordHash = SecurityUtils.HashPassword(request.Password),
            AssignedClinicianId = request.Role == UserRole.Patient ? request.AssignedClinicianId : null
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            AssignedClinicianId = user.AssignedClinicianId
        };
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Include(u => u.AssignedClinician)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        return users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            AssignedClinicianId = u.AssignedClinicianId,
            AssignedClinicianName = u.AssignedClinician?.FullName,
            DateOfBirth = u.DateOfBirth,
            PhoneNumber = u.PhoneNumber,
            Address = u.Address
        }).ToList();
    }

    public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    private string GetJwtSecret()
        => Environment.GetEnvironmentVariable("JWT_SECRET")
           ?? _configuration["Jwt:Secret"]
           ?? AppConstants.DefaultJwtSecret;

    private int GetJwtExpiryDays()
        => int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_DAYS"), out var days)
            ? days
            : int.TryParse(_configuration["Jwt:ExpiryDays"], out var configDays)
                ? configDays
                : AppConstants.JwtExpiryDays;
}

