using System.ComponentModel.DataAnnotations;

namespace FitBit.Api.Dtos.Auth;

// No docType and no userId anywhere in these DTOs — both are set server-side,
// which makes over-posting structurally impossible rather than merely guarded.

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = default!;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = default!;

    [Required, MaxLength(60)]
    public string DisplayName { get; set; } = default!;
}

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(128)]
    public string Password { get; set; } = default!;
}

public sealed record UserResponse(string Id, string Email, string DisplayName, DateTime CreatedAt);

public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserResponse User);
