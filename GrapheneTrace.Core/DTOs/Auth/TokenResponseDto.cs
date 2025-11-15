using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Auth;

public class TokenResponseDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
}

