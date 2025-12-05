using Matrix.Identity.Api.Contracts.Requests;
using Matrix.Identity.Api.Contracts.Responses;
using Matrix.Identity.Application.UseCases.Auth.LoginUser;
using Matrix.Identity.Application.UseCases.Auth.RefreshToken;
using Matrix.Identity.Application.UseCases.Auth.RegisterUser;
using Matrix.Identity.Application.UseCases.Auth.RevokeRefreshToken;
using Matrix.Identity.Application.UseCases.Sessions.GetUserSessions;
using Matrix.Identity.Application.UseCases.Sessions.RevokeAllUserSessions;
using Matrix.Identity.Application.UseCases.Sessions.RevokeUserSession;
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

        #region [ Register & Login ]

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

            // берём user-agent и ip из HTTP-контекста
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var command = new LoginUserCommand(
                request.Login,
                request.Password,
                request.DeviceId,
                request.DeviceName,
                userAgent,
                ipAddress
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

        #endregion [ Register & Login ]

        #region [ Refresh & Logout ]

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken cancellationToken)
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var command = new RefreshTokenCommand(
                request.RefreshToken,
                request.DeviceId,
                userAgent,
                ipAddress
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

        #endregion [ Refresh & Logout ]

        #region [ Sessions ]

        [Authorize]
        [HttpGet("sessions")]
        public async Task<ActionResult<List<SessionResponse>>> GetSessions(
            CancellationToken cancellationToken)
        {
            var userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var query = new GetUserSessionsQuery(userId);

            var sessions = await _sender.Send(query, cancellationToken);

            var response = sessions
                .Select(s => new SessionResponse
                {
                    Id = s.Id,
                    DeviceId = s.DeviceId,
                    DeviceName = s.DeviceName,
                    UserAgent = s.UserAgent,
                    IpAddress = s.IpAddress,
                    Country = s.Country,
                    Region = s.Region,
                    City = s.City,
                    CreatedAtUtc = s.CreatedAtUtc,
                    LastUsedAtUtc = s.LastUsedAtUtc,
                    IsActive = s.IsActive
                })
                .ToList();

            return Ok(response);
        }

        [Authorize]
        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            var userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var command = new RevokeUserSessionCommand(userId, sessionId);

            await _sender.Send(command, cancellationToken);

            // Даже если sessionId не нашёлся – всё равно 204, запрос идемпотентный
            return NoContent();
        }

        [Authorize]
        [HttpDelete("sessions")]
        public async Task<IActionResult> RevokeAllSessions(
            CancellationToken cancellationToken)
        {
            var userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var command = new RevokeAllUserSessionsCommand(userId);

            await _sender.Send(command, cancellationToken);

            // Idempotent: даже если все токены уже были отозваны, просто возвращаем 204
            return NoContent();
        }

        #endregion [ Sessions ]

        #region [ Me ]

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

        #endregion [ Me ]
    }
}