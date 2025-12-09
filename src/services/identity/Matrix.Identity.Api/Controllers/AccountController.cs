using Matrix.Identity.Api.Contracts.Requests;
using Matrix.Identity.Api.Contracts.Responses;
using Matrix.Identity.Application.UseCases.Account.ChangeAvatarFromFile;
using Matrix.Identity.Application.UseCases.Account.ChangePassword;
using Matrix.Identity.Application.UseCases.Account.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/internal/[controller]/{userId:guid}")]
    public class AccountController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        #region [ Profile ]

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var query = new GetUserProfileQuery(userId);

            UserProfileResult result = await _sender.Send(request: query, cancellationToken: cancellationToken);

            var response = new UserProfileResponse
            {
                UserId = result.UserId,
                Email = result.Email,
                Username = result.Username,
                AvatarUrl = result.AvatarUrl
            };

            return Ok(response);
        }

        #endregion [ Profile ]

        #region [ Avatar & Password ]

        [HttpPut("avatar")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ChangeAvatarResponse>> ChangeAvatar(
            [FromRoute] Guid userId,
            IFormFile? avatar,
            CancellationToken cancellationToken)
        {
            if (avatar is null || avatar.Length == 0) return BadRequest("Avatar file is required.");

            await using Stream stream = avatar.OpenReadStream();

            var command = new ChangeAvatarFromFileCommand(
                UserId: userId,
                FileStream: stream,
                FileName: avatar.FileName,
                ContentType: avatar.ContentType ?? "image/png"
            );

            string newAvatarPath = await _sender.Send(request: command, cancellationToken: cancellationToken);

            var response = new ChangeAvatarResponse(newAvatarPath);
            return Ok(response);
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromRoute] Guid userId,
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ChangePasswordCommand(
                UserId: userId,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword);

            await _sender.Send(request: command, cancellationToken: cancellationToken);

            return NoContent();
        }

        #endregion [ Avatar & Password ]
    }
}
