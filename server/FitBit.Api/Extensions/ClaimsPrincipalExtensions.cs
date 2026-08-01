using System.Security.Claims;

namespace FitBit.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// We set MapInboundClaims = false so "sub" arrives verbatim, but the fallback
    /// to NameIdentifier keeps this working if that ever flips — mixing the two
    /// conventions is the most common JWT bug in ASP.NET Core.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Token is missing the subject claim.");
}
