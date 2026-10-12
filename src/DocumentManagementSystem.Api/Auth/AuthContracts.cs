using System.ComponentModel.DataAnnotations;

namespace DocumentManagementSystem.Api.Auth;

public sealed record LoginRequest(
    [property: Required] string Username,
    [property: Required] string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string TokenType = "Bearer");
