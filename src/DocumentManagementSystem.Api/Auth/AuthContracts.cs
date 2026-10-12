namespace DocumentManagementSystem.Api.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Username,
    string TokenType = "Bearer");
