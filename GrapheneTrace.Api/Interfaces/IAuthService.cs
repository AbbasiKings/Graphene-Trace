using GrapheneTrace.Core.DTOs.Admin;
using GrapheneTrace.Core.DTOs.Auth;
using GrapheneTrace.Core.Models;

namespace GrapheneTrace.Api.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(PasswordResetDto request, CancellationToken cancellationToken = default);
    Task<AdminUserDto?> CreateUserAsync(NewUserDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

