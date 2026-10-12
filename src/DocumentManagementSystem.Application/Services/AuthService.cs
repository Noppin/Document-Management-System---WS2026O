using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace DocumentManagementSystem.Application.Services;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher<User> passwordHasher,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var username = request.Username.Trim();
        var normalizedUsername = Normalize(username);

        if (users.ExistsByNormalizedUsername(normalizedUsername))
        {
            return new RegistrationResult(false, null, "username_taken");
        }

        var user = new User
        {
            Username = username,
            NormalizedUsername = normalizedUsername
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        users.Add(user);

        return new RegistrationResult(true, new AuthenticatedUser(user.Id, user.Username));
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = users.GetByNormalizedUsername(Normalize(request.Username));
        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.Username);
    }

    private static string Normalize(string username) => username.Trim().ToUpperInvariant();
}
