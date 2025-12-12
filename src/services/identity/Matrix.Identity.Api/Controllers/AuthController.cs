using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/internal/[controller]")]
    public sealed class AuthController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        #region [ Register & Login ]

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RegisterUserCommand(
                Email: request.Email,
                Username: request.Username,
                Password: request.Password
            );

            RegisterUserResult result = await _sender.Send(request: command, cancellationToken: cancellationToken);

            var response = new RegisterResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                Username = result.Username
            };

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            // берём user-agent и ip из HTTP-контекста
            string userAgent = Request.Headers.UserAgent.ToString();

            string? ipAddress = null;
            if (Request.Headers.TryGetValue(key: "X-Real-IP", value: out StringValues realIpHeader))
                ipAddress = realIpHeader.ToString();

            // fallback на RemoteIpAddress, если что-то пошло не так
            ipAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString();

            var command = new LoginUserCommand(
                Login: request.Login,
                Password: request.Password,
                DeviceId: request.DeviceId,
                DeviceName: request.DeviceName,
                UserAgent: userAgent,
                IpAddress: ipAddress,
                RememberMe: request.RememberMe
            );

            LoginUserResult result = await _sender.Send(request: command, cancellationToken: cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = result.AccessToken,
                TokenType = result.TokenType,
                ExpiresIn = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
                IsPersistent = result.IsPersistent
            };

            return Ok(response);
        }

        #endregion [ Register & Login ]

        #region [ Refresh & Logout ]

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken cancellationToken)
        {
            string userAgent = Request.Headers.UserAgent.ToString();

            string? ipAddress = null;
            if (Request.Headers.TryGetValue(key: "X-Real-IP", value: out StringValues realIpHeader))
                ipAddress = realIpHeader.ToString();

            // fallback на RemoteIpAddress, если что-то пошло не так
            ipAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString();

            var command = new RefreshTokenCommand(
                RefreshToken: request.RefreshToken,
                DeviceId: request.DeviceId,
                UserAgent: userAgent,
                IpAddress: ipAddress
            );

            LoginUserResult result = await _sender.Send(request: command, cancellationToken: cancellationToken);

            var response = new LoginResponse
            {
                AccessToken = result.AccessToken,
                TokenType = result.TokenType,
                ExpiresIn = result.AccessTokenExpiresInSeconds,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
                IsPersistent = result.IsPersistent
            };

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RevokeRefreshTokenCommand(request.RefreshToken);
            await _sender.Send(request: command, cancellationToken: cancellationToken);
            return NoContent();
        }

        #endregion [ Refresh & Logout ]

        #region [ Sessions ]

        [HttpGet("sessions")]
        public async Task<ActionResult<List<SessionResponse>>> GetSessions(
            CancellationToken cancellationToken)
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            var query = new GetUserSessionsQuery(userId);

            IReadOnlyCollection<UserSessionResult> sessions =
                await _sender.Send(request: query, cancellationToken: cancellationToken);

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

        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            var command = new RevokeUserSessionCommand(UserId: userId, SessionId: sessionId);

            await _sender.Send(request: command, cancellationToken: cancellationToken);

            // Даже если sessionId не нашёлся – всё равно 204, запрос идемпотентный
            return NoContent();
        }

        [HttpDelete("sessions")]
        public async Task<IActionResult> RevokeAllSessions(
            CancellationToken cancellationToken)
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            var command = new RevokeAllUserSessionsCommand(userId);

            await _sender.Send(request: command, cancellationToken: cancellationToken);

            // Idempotent: даже если все токены уже были отозваны, просто возвращаем 204
            return NoContent();
        }

        #endregion [ Sessions ]
    }
}
