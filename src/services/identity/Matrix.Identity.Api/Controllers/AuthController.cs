using Matrix.Identity.Application.UseCases.Auth.LoginUser;
using Matrix.Identity.Application.UseCases.Auth.RefreshToken;
using Matrix.Identity.Application.UseCases.Auth.RegisterUser;
using Matrix.Identity.Application.UseCases.Auth.RevokeRefreshToken;
using Matrix.Identity.Contracts.Auth.Requests;
using Matrix.Identity.Contracts.Auth.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Matrix.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController(ISender sender) : ControllerBase
    {
        #region [ Fields ]

        private readonly ISender _sender = sender;

        #endregion [ Fields ]

        #region [ Register ]

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RegisterUserCommand(
                Email: request.Email,
                Username: request.Username,
                Password: request.Password);

            RegisterUserResult result = await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            var response = new RegisterResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                Username = result.Username
            };

            return Ok(response);
        }

        #endregion [ Register ]

        #region [ Login ]

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            // берём user-agent и ip из HTTP-контекста
            string userAgent = Request.Headers.UserAgent.ToString();

            string? ipAddress = null;
            if (Request.Headers.TryGetValue(
                    key: "X-Real-IP",
                    value: out StringValues realIpHeader))
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
                RememberMe: request.RememberMe);

            LoginUserResult result = await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

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

        #endregion [ Login ]

        #region [ Refresh ]

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken cancellationToken)
        {
            string userAgent = Request.Headers.UserAgent.ToString();

            string? ipAddress = null;
            if (Request.Headers.TryGetValue(
                    key: "X-Real-IP",
                    value: out StringValues realIpHeader))
                ipAddress = realIpHeader.ToString();

            // fallback на RemoteIpAddress, если что-то пошло не так
            ipAddress ??= HttpContext.Connection.RemoteIpAddress?.ToString();

            var command = new RefreshTokenCommand(
                RefreshToken: request.RefreshToken,
                DeviceId: request.DeviceId,
                UserAgent: userAgent,
                IpAddress: ipAddress);

            LoginUserResult result = await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

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

        #endregion [ Refresh ]

        #region [ Logout ]

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RevokeRefreshTokenCommand(request.RefreshToken);
            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);
            return NoContent();
        }

        #endregion [ Logout ]
    }
}
