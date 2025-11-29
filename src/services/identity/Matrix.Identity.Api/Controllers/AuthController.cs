using Matrix.Identity.Api.Contracts;
using Matrix.Identity.Application.UseCases.LoginUser;
using Matrix.Identity.Application.UseCases.RefreshToken;
using Matrix.Identity.Application.UseCases.RegisterUser;
using Matrix.Identity.Application.UseCases.RevokeRefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Matrix.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var command = new RegisterUserCommand(
                request.Email,
                request.Username,
                request.Password,
                request.ConfirmPassword
            );

            var result = await _sender.Send(command, cancellationToken);

            var response = new RegisterResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                Username = result.Username
            };

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var command = new LoginUserCommand(
                request.Login,
                request.Password
            );

            var result = await _sender.Send(command, cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = result.AccessToken,
                TokenType = result.TokenType,
                ExpiresIn = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc
            };

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RefreshTokenCommand(request.RefreshToken);

            var result = await _sender.Send(command, cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = result.AccessToken,
                ExpiresIn = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc
            };

            return Ok(response);
        }

        [AllowAnonymous] // refresh/logout у нас по refresh-токену, access необязателен
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RevokeRefreshTokenCommand(request.RefreshToken);
            await _sender.Send(command, cancellationToken);
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<MeResponse> Me()
        {
            var userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            var emailClaim =
                User.FindFirst(JwtRegisteredClaimNames.Email) ??
                User.FindFirst(ClaimTypes.Email);

            var usernameClaim =
                User.FindFirst(JwtRegisteredClaimNames.UniqueName) ??
                User.FindFirst(ClaimTypes.Name);

            if (userIdClaim is null || emailClaim is null || usernameClaim is null)
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var response = new MeResponse
            {
                UserId = userId,
                Email = emailClaim.Value,
                Username = usernameClaim.Value
            };

            return Ok(response);
        }
    }
}