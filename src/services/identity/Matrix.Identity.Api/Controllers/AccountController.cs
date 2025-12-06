using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.Identity.Api.Contracts.Requests;
using Matrix.Identity.Application.UseCases.Account.ChangeAvatar;
using Matrix.Identity.Application.UseCases.Account.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpPut("avatar")]
        public async Task<IActionResult> ChangeAvatar(
            [FromBody] ChangeAvatarRequest request,
            CancellationToken cancellationToken)
        {
            Guid? userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            // аватарка может быть null (сброс)
            var command = new ChangeAvatarCommand(UserId: userId.Value, AvatarUrl: request.AvatarUrl);

            await _sender.Send(request: command, cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            Guid? userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var command = new ChangePasswordCommand(
                UserId: userId.Value,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword,
                ConfirmPassword: request.ConfirmPassword);

            await _sender.Send(request: command, cancellationToken: cancellationToken);

            return NoContent();
        }

        private Guid? GetCurrentUserId()
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null) return null;

            if (!Guid.TryParse(input: userIdClaim.Value, result: out Guid userId)) return null;

            return userId;
        }
    }
}
