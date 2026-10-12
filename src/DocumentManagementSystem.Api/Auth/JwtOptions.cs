namespace DocumentManagementSystem.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "DocumentManagementSystem.Api";
    public string Audience { get; init; } = "DocumentManagementSystem.Frontend";
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}
