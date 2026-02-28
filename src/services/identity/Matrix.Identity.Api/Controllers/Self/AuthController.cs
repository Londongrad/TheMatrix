using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword;
using Matrix.Identity.Application.UseCases.Self.Auth.RefreshToken;
using Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser;
using Matrix.Identity.Application.UseCases.Self.Auth.RevokeRefreshToken;
using Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset;
using Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmail;
using Matrix.Identity.Application.UseCases.Self.Account.ConfirmEmailChange;
using Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation;
using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Matrix.Identity.Api.Controllers.Self
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

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

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken)
        {
            var command = new LoginUserCommand(
                Login: request.Login,
                Password: request.Password,
                DeviceId: request.DeviceId,
                DeviceName: request.DeviceName,
                UserAgent: GetUserAgent(),
                IpAddress: GetIpAddress(),
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

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RefreshTokenCommand(
                RefreshToken: request.RefreshToken,
                DeviceId: request.DeviceId,
                UserAgent: GetUserAgent(),
                IpAddress: GetIpAddress());

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

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RevokeRefreshTokenCommand(
                RefreshToken: request.RefreshToken,
                IpAddress: GetIpAddress(),
                UserAgent: GetUserAgent());

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("email-confirmation/send")]
        public async Task<IActionResult> SendEmailConfirmation(
            [FromBody] SendEmailConfirmationRequest request,
            CancellationToken cancellationToken)
        {
            var command = new SendEmailConfirmationCommand(
                Email: request.Email,
                IpAddress: GetIpAddress(),
                UserAgent: GetUserAgent());

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("email-confirmation/confirm")]
        public async Task<IActionResult> ConfirmEmail(
            [FromBody] ConfirmEmailRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ConfirmEmailCommand(
                UserId: request.UserId,
                Token: request.Token,
                IpAddress: GetIpAddress(),
                UserAgent: GetUserAgent());

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("email-change/confirm")]
        public async Task<IActionResult> ConfirmEmailChange(
            [FromBody] ConfirmEmailChangeRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ConfirmEmailChangeCommand(
                UserId: request.UserId,
                Token: request.Token,
                IpAddress: GetIpAddress(),
                UserAgent: GetUserAgent());

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("password/forgot")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var command = new SendPasswordResetCommand(
                Email: request.Email,
                IpAddress: GetIpAddress(),
                UserAgent: GetUserAgent());

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ResetPasswordCommand(
                UserId: request.UserId,
                Token: request.Token,
                NewPassword: request.NewPassword,
                IpAddress: GetIpAddress(),
                UserAgent: GetUserAgent());

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        private string GetUserAgent()
        {
            return Request.Headers.UserAgent.ToString();
        }

        private string? GetIpAddress()
        {
            if (Request.Headers.TryGetValue(
                    key: "X-Real-IP",
                    value: out StringValues realIpHeader))
                return realIpHeader.ToString();

            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
