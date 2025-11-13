using GrapheneTrace.Core.DataTransferObjects;
using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Client.Services
{
    public class AuthService
    {
        private bool _isAuthenticated = false;
        private UserRole? _role = null;
        private string? _userEmail = null;
        private string? _userName = null;
        private string? _token = null;

        public bool IsAuthenticated => _isAuthenticated;
        public UserRole? Role => _role;
        public string? UserEmail => _userEmail;
        public string? UserName => _userName;
        public string? Token => _token;

        public event Action? AuthenticationStateChanged;

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto requestDto)
        {
            await Task.Delay(100);

            var response = requestDto.Email.ToLower() switch
            {
                "admin@gmail.com" when requestDto.Password == "admin123" => new LoginResponseDto
                {
                    Status = true,
                    Message = "Login successful",
                    Token = "mock-jwt-token-admin",
                    Role = UserRole.Admin,
                    UserName = "Admin User"
                },
                "clinician@gmail.com" when requestDto.Password == "clinician123" => new LoginResponseDto
                {
                    Status = true,
                    Message = "Login successful",
                    Token = "mock-jwt-token-clinician",
                    Role = UserRole.Clinician,
                    UserName = "Clinician User"
                },
                "patient@gmail.com" when requestDto.Password == "patient123" => new LoginResponseDto
                {
                    Status = true,
                    Message = "Login successful",
                    Token = "mock-jwt-token-patient",
                    Role = UserRole.Patient,
                    UserName = "Patient User"
                },
                _ => new LoginResponseDto
                {
                    Status = false,
                    Message = "Invalid email or password.",
                    Token = string.Empty,
                    Role = UserRole.Patient,
                    UserName = string.Empty
                }
            };

            if (response.Status)
            {
                SetAuthenticationState(requestDto.Email, response.Role, response.UserName, response.Token);
            }

            return response;
        }

        private void SetAuthenticationState(string email, UserRole role, string userName, string token)
        {
            _isAuthenticated = true;
            _userEmail = email;
            _role = role;
            _userName = userName;
            _token = token;
            AuthenticationStateChanged?.Invoke();
        }

        public void Logout()
        {
            _isAuthenticated = false;
            _userEmail = null;
            _role = null;
            _userName = null;
            _token = null;
            AuthenticationStateChanged?.Invoke();
        }

        public bool HasRole(UserRole role)
        {
            return _isAuthenticated && _role == role;
        }

        public bool HasAnyRole(params UserRole[] roles)
        {
            return _isAuthenticated && _role.HasValue && roles.Contains(_role.Value);
        }
    }
}

