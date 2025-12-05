using Matrix.Identity.Api.Contracts.Requests;
using Matrix.Identity.Application.UseCases.Account.ChangeAvatar;
using Matrix.Identity.Application.UseCases.Account.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            // аватарка может быть null (сброс)
            var command = new ChangeAvatarCommand(userId.Value, request.AvatarUrl);

            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var command = new ChangePasswordCommand(
                userId.Value,
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmPassword);

            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null)
            {
                return null;
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}