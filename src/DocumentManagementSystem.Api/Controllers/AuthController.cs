using DocumentManagementSystem.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DocumentManagementSystem.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    JwtTokenService tokenService,
    IOptions<JwtOptions> options) : ControllerBase
{
    private readonly JwtOptions jwt = options.Value;

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var validUsername = string.Equals(request.Username, jwt.Username, StringComparison.Ordinal);
        var validPassword = string.Equals(request.Password, jwt.Password, StringComparison.Ordinal);

        if (!validUsername || !validPassword)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = "The supplied username or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(tokenService.CreateToken(request.Username));
    }
}
