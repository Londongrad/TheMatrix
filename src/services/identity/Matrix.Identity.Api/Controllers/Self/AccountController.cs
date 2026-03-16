using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Self.Account.CancelPendingEmailChange;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeAvatarFromFile;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName;
using Matrix.Identity.Application.UseCases.Self.Account.ChangePassword;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername;
using Matrix.Identity.Application.UseCases.Self.Account.ClearAvatar;
using Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount;
using Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Matrix.Identity.Application.UseCases.Self.Account.RequestEmailChange;
using Matrix.Identity.Application.UseCases.Self.Account.ResendPendingEmailChange;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Matrix.Identity.Api.Controllers.Self
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

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
                PendingEmail = result.PendingEmail,
                Username = result.Username,
                DisplayName = result.DisplayName,
                AvatarUrl = result.AvatarUrl,
                IsEmailConfirmed = result.IsEmailConfirmed,
                CreatedAtUtc = result.CreatedAtUtc,
                EmailConfirmedAtUtc = result.EmailConfirmedAtUtc,
                EffectivePermissions = result.EffectivePermissions.ToArray(),
                PermissionsVersion = result.PermissionsVersion
            };

            return Ok(response);
        }

        [HttpGet("security-activity")]
        public async Task<ActionResult<PagedResult<SecurityActivityItemResponse>>> GetSecurityActivity(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken cancellationToken = default)
        {
            var query = new GetMySecurityActivityQuery(
                new Pagination(
                    pageNumber: pageNumber,
                    pageSize: pageSize));

            PagedResult<SecurityActivityItemResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var response = new PagedResult<SecurityActivityItemResponse>(
                items: result.Items.Select(item => new SecurityActivityItemResponse
                    {
                        EventType = item.EventType.ToString(),
                        IsSuccessful = item.IsSuccessful,
                        OccurredAtUtc = item.OccurredAtUtc,
                        IpAddress = item.IpAddress,
                        UserAgent = item.UserAgent,
                        DeviceId = item.DeviceId,
                        DeviceName = item.DeviceName,
                        Details = item.Details
                    })
                   .ToList(),
                totalCount: result.TotalCount,
                pageNumber: result.PageNumber,
                pageSize: result.PageSize);

            return Ok(response);
        }

        #endregion [ Profile ]

        #region [ Identity Updates ]

        [HttpPut("display-name")]
        public async Task<ActionResult<ChangeDisplayNameResponse>> ChangeDisplayName(
            [FromBody] ChangeDisplayNameRequest request,
            CancellationToken cancellationToken)
        {
            string? displayName = await _sender.Send(
                request: new ChangeDisplayNameCommand(request.DisplayName),
                cancellationToken: cancellationToken);

            var response = new ChangeDisplayNameResponse
            {
                DisplayName = displayName
            };

            return Ok(response);
        }

        [HttpPut("username")]
        public async Task<ActionResult<ChangeUsernameResponse>> ChangeUsername(
            [FromBody] ChangeUsernameRequest request,
            CancellationToken cancellationToken)
        {
            string username = await _sender.Send(
                request: new ChangeUsernameCommand(
                    Username: request.Username,
                    CurrentPassword: request.CurrentPassword),
                cancellationToken: cancellationToken);

            var response = new ChangeUsernameResponse
            {
                Username = username
            };

            return Ok(response);
        }

        [HttpPut("email")]
        public async Task<ActionResult<ChangeEmailResponse>> ChangeEmail(
            [FromBody] ChangeEmailRequest request,
            CancellationToken cancellationToken)
        {
            string pendingEmail = await _sender.Send(
                request: new RequestEmailChangeCommand(
                    NewEmail: request.NewEmail,
                    CurrentPassword: request.CurrentPassword,
                    IpAddress: GetIpAddress(),
                    UserAgent: GetUserAgent()),
                cancellationToken: cancellationToken);

            var response = new ChangeEmailResponse
            {
                PendingEmail = pendingEmail
            };

            return Ok(response);
        }

        [HttpPost("email/pending/resend")]
        public async Task<IActionResult> ResendPendingEmailChange(CancellationToken cancellationToken)
        {
            await _sender.Send(
                request: new ResendPendingEmailChangeCommand(
                    IpAddress: GetIpAddress(),
                    UserAgent: GetUserAgent()),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpDelete("email/pending")]
        public async Task<IActionResult> CancelPendingEmailChange(CancellationToken cancellationToken)
        {
            await _sender.Send(
                request: new CancelPendingEmailChangeCommand(
                    IpAddress: GetIpAddress(),
                    UserAgent: GetUserAgent()),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPut("avatar")]
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

        [HttpDelete("avatar")]
        public async Task<ActionResult<ChangeAvatarResponse>> ClearAvatar(CancellationToken cancellationToken)
        {
            await _sender.Send(
                request: new ClearAvatarCommand(),
                cancellationToken: cancellationToken);

            var response = new ChangeAvatarResponse
            {
                AvatarUrl = null
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

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAccount(
            [FromBody] DeleteAccountRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                request: new DeleteMyAccountCommand(
                    CurrentPassword: request.CurrentPassword,
                    IpAddress: GetIpAddress(),
                    UserAgent: GetUserAgent()),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        #endregion [ Identity Updates ]
    }
}
