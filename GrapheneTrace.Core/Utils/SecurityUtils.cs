using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GrapheneTrace.Core.Constants;
using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;

namespace GrapheneTrace.Core.Utils;

public static class SecurityUtils
{
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(AppConstants.PasswordSaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            AppConstants.PasswordIterations,
            HashAlgorithmName.SHA256,
            AppConstants.PasswordKeySize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            AppConstants.PasswordIterations,
            HashAlgorithmName.SHA256,
            AppConstants.PasswordKeySize);

        return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
    }

    public static string GenerateToken(User user, string secret, int expiryDays)
    {
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["role"] = user.Role.ToString(),
            ["name"] = user.FullName,
            ["email"] = user.Email,
            ["exp"] = DateTimeOffset.UtcNow.AddDays(expiryDays).ToUnixTimeSeconds(),
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var headerSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var unsignedToken = $"{headerSegment}.{payloadSegment}";
        var signatureSegment = Base64UrlEncode(Sign(unsignedToken, secret));

        return $"{unsignedToken}.{signatureSegment}";
    }

    public static JwtValidationResult ValidateToken(string token, string secret)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return JwtValidationResult.Invalid("Token missing");
        }

        var segments = token.Split('.');
        if (segments.Length != 3)
        {
            return JwtValidationResult.Invalid("Token malformed");
        }

        var unsignedToken = $"{segments[0]}.{segments[1]}";
        var expectedSignature = Base64UrlEncode(Sign(unsignedToken, secret));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(segments[2])))
        {
            return JwtValidationResult.Invalid("Signature mismatch");
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(segments[1]));
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
        if (payload is null || !payload.TryGetValue("exp", out var expElement))
        {
            return JwtValidationResult.Invalid("Payload missing exp");
        }

        var expSeconds = expElement.GetInt64();
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expSeconds)
        {
            return JwtValidationResult.Invalid("Token expired");
        }

        var result = new JwtValidationResult
        {
            IsValid = true,
            UserId = payload.TryGetValue("sub", out var sub)
                ? Guid.Parse(sub.GetString() ?? Guid.Empty.ToString())
                : Guid.Empty,
            Role = payload.TryGetValue("role", out var roleEl)
                ? roleEl.GetString() ?? UserRole.Patient.ToString()
                : UserRole.Patient.ToString(),
            Name = payload.TryGetValue("name", out var nameEl) ? nameEl.GetString() : string.Empty,
            Email = payload.TryGetValue("email", out var emailEl) ? emailEl.GetString() : string.Empty
        };

        return result;
    }

    private static byte[] Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 2:
                output += "==";
                break;
            case 3:
                output += "=";
                break;
        }

        return Convert.FromBase64String(output);
    }
}

public sealed class JwtValidationResult
{
    public bool IsValid { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = UserRole.Patient.ToString();
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Error { get; set; }

    public static JwtValidationResult Invalid(string error) => new()
    {
        IsValid = false,
        Error = error
    };
}

