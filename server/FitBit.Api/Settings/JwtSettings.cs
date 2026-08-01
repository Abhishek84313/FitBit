namespace FitBit.Api.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "FitBit.Api";
    public string Audience { get; set; } = "FitBit.Client";
    public int ExpiryMinutes { get; set; } = 720;
}
