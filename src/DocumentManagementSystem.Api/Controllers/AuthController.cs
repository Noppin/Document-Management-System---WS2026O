using DocumentManagementSystem.Api.Auth;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("auth")]
public sealed class AuthController(
    IAuthService authService,
    JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);

        if (!result.Succeeded || result.User is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Username already exists",
                Detail = "Choose a different username.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return StatusCode(StatusCodes.Status201Created, tokenService.CreateToken(result.User));
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.AuthenticateAsync(request, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "The supplied username or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(tokenService.CreateToken(user));
    }
}
