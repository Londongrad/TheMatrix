using Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile;
using Matrix.Identity.Application.UseCases.Self.Account.ChangePassword;
using Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Self
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        #region [ Profile ]

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile(CancellationToken cancellationToken)
        {
            var query = new GetMyProfileQuery();

            MyProfileResult result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

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
            IFormFile? avatar,
            CancellationToken cancellationToken)
        {
            if (avatar is null || avatar.Length == 0)
                return BadRequest("Avatar file is required.");

            await using Stream stream = avatar.OpenReadStream();

            var command = new ChangeAvatarFromFileCommand(
                FileStream: stream,
                FileName: avatar.FileName,
                ContentType: avatar.ContentType ?? "image/png");

            string newAvatarPath = await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            var response = new ChangeAvatarResponse
            {
                AvatarUrl = newAvatarPath
            };
            return Ok(response);
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            var command = new ChangePasswordCommand(
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        #endregion [ Avatar & Password ]
    }
}
