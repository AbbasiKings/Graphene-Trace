using System.Net.Http.Headers;
using System.Net.Http.Json;
using GrapheneTrace.Core.DTOs.Auth;
using GrapheneTrace.Core.Enums;
using Microsoft.JSInterop;
using System.Text.Json;

namespace GrapheneTrace.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    private bool _isAuthenticated;
    private UserRole? _role;
    private string? _userEmail;
    private string? _userName;
    private string? _token;
    private Guid? _userId;

    private const string StorageKey = "graphene_trace_auth";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
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
                await SetAuthenticationStateAsync(requestDto.Email, payload.Role, payload.UserName, payload.Token, payload.UserId);
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

    private async Task SetAuthenticationStateAsync(string email, UserRole role, string userName, string token, Guid userId)
    {
        _isAuthenticated = true;
        _userEmail = email;
        _role = role;
        _userName = userName;
        _token = token;
        _userId = userId;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Save to localStorage
        await SaveAuthStateAsync(email, role, userName, token, userId);
        
        AuthenticationStateChanged?.Invoke();
    }

    private async Task SaveAuthStateAsync(string email, UserRole role, string userName, string token, Guid userId)
    {
        try
        {
            var authData = new
            {
                Email = email,
                Role = role.ToString(),
                UserName = userName,
                Token = token,
                UserId = userId.ToString()
            };
            
            var json = JsonSerializer.Serialize(authData);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    public async Task LoadAuthStateAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var authData = JsonSerializer.Deserialize<AuthData>(json);
            if (authData == null || string.IsNullOrWhiteSpace(authData.Token))
            {
                return;
            }

            // Restore authentication state
            if (Enum.TryParse<UserRole>(authData.Role, out var role))
            {
                _isAuthenticated = true;
                _userEmail = authData.Email;
                _role = role;
                _userName = authData.UserName;
                _token = authData.Token;
                _userId = Guid.TryParse(authData.UserId, out var userId) ? userId : null;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);
                AuthenticationStateChanged?.Invoke();
            }
        }
        catch
        {
            // Ignore errors - user will need to login again
        }
    }

    private class AuthData
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public async Task LogoutAsync()
    {
        _isAuthenticated = false;
        _userEmail = null;
        _role = null;
        _userName = null;
        _token = null;
        _userId = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        
        // Clear localStorage
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch
        {
            // Ignore errors
        }
        
        AuthenticationStateChanged?.Invoke();
    }

    public bool HasRole(UserRole role) => _isAuthenticated && _role == role;

    public bool HasAnyRole(params UserRole[] roles)
        => _isAuthenticated && _role.HasValue && roles.Contains(_role.Value);
}
