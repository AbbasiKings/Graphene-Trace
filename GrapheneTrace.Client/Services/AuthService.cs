using System.Net.Http.Headers;
using System.Net.Http.Json;
using GrapheneTrace.Core.DTOs.Auth;
using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    private bool _isAuthenticated;
    private UserRole? _role;
    private string? _userEmail;
    private string? _userName;
    private string? _token;
    private Guid? _userId;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsAuthenticated => _isAuthenticated;
    public UserRole? Role => _role;
    public string? UserEmail => _userEmail;
    public string? UserName => _userName;
    public string? Token => _token;
    public Guid? UserId => _userId;

    public event Action? AuthenticationStateChanged;

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto requestDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", requestDto);
            var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>() ?? new LoginResponseDto
            {
                Status = false,
                Message = "Unexpected response from server."
            };

            if (response.IsSuccessStatusCode && payload.Status)
            {
                SetAuthenticationState(requestDto.Email, payload.Role, payload.UserName, payload.Token, payload.UserId);
            }
            else
            {
                payload.Status = false;
                payload.Message = !string.IsNullOrWhiteSpace(payload.Message)
                    ? payload.Message
                    : "Invalid email or password.";
            }

            return payload;
        }
        catch (Exception)
        {
            return new LoginResponseDto
            {
                Status = false,
                Message = "Unable to reach GrapheneTrace API. Please verify the backend is running."
            };
        }
    }

    private void SetAuthenticationState(string email, UserRole role, string userName, string token, Guid userId)
    {
        _isAuthenticated = true;
        _userEmail = email;
        _role = role;
        _userName = userName;
        _token = token;
        _userId = userId;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        AuthenticationStateChanged?.Invoke();
    }

    public void Logout()
    {
        _isAuthenticated = false;
        _userEmail = null;
        _role = null;
        _userName = null;
        _token = null;
        _userId = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        AuthenticationStateChanged?.Invoke();
    }

    public bool HasRole(UserRole role) => _isAuthenticated && _role == role;

    public bool HasAnyRole(params UserRole[] roles)
        => _isAuthenticated && _role.HasValue && roles.Contains(_role.Value);
}
