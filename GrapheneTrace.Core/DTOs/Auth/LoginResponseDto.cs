using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Auth;

public class LoginResponseDto
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public string UserName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}

