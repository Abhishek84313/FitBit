using FitBit.Api.Dtos.Auth;
using FitBit.Api.Infrastructure;
using FitBit.Api.Models;
using FitBit.Api.Repositories;
using MongoDB.Driver;

namespace FitBit.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<UserResponse> GetCurrentAsync(string userId, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    /// <summary>
    /// A real BCrypt hash of a throwaway value. When the email is unknown we still
    /// run a verify against it, so the response time doesn't reveal whether the
    /// account exists.
    /// </summary>
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("not-a-real-password-timing-equaliser");

    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public AuthService(IUserRepository users, ITokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _users.GetByEmailAsync(email, ct) is not null)
            throw ApiException.Conflict("That email is already registered.");

        var user = new UserDocument
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        try
        {
            await _users.CreateAsync(user, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // The unique index is the real guard; the check above just gives a
            // nicer message in the common case.
            throw ApiException.Conflict("That email is already registered.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        // Identical failure for "unknown email" and "wrong password" — a different
        // message on either branch is a user-enumeration oracle.
        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyHash);
            throw ApiException.Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw ApiException.Unauthorized();

        return BuildAuthResponse(user);
    }

    public async Task<UserResponse> GetCurrentAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw ApiException.NotFound("User not found.");
        return ToResponse(user);
    }

    private AuthResponse BuildAuthResponse(UserDocument user)
    {
        var (token, expiresAt) = _tokens.Create(user);
        return new AuthResponse(token, expiresAt, ToResponse(user));
    }

    // Note the shape: PasswordHash has no route out of this class.
    private static UserResponse ToResponse(UserDocument user) =>
        new(user.Id, user.Email, user.DisplayName, user.CreatedAtUtc);
}
