namespace DocumentManagementSystem.Application.Dtos;

public sealed record RegisterRequest(string Username, string Password);
public sealed record LoginRequest(string Username, string Password);
public sealed record AuthenticatedUser(Guid Id, string Username);
public sealed record RegistrationResult(bool Succeeded, AuthenticatedUser? User, string? ErrorCode = null);
