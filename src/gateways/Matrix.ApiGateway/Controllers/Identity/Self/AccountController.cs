using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Common.Urls;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.BuildingBlocks.Api.Errors;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Matrix.ApiGateway.Controllers.Identity.Self
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AccountController(
        IIdentityAccountClient identityAccountClient,
        IIdentityAssetsClient identityAssetsClient,
        IDistributedCache distributedCache) : ControllerBase
    {
        private readonly IDistributedCache _distributedCache = distributedCache;
        private readonly IIdentityAccountClient _identityAccountClient = identityAccountClient;
        private readonly IIdentityAssetsClient _identityAssetsClient = identityAssetsClient;

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile(CancellationToken cancellationToken)
        {
            UserProfileResponse profile =
                await _identityAccountClient.GetProfileAsync(cancellationToken);

            profile.AvatarUrl = Request.ToPublicUrl(profile.AvatarUrl);

            return Ok(profile);
        }

        [HttpGet("security-activity")]
        public async Task<ActionResult<PagedResult<SecurityActivityItemResponse>>> GetSecurityActivity(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken cancellationToken = default)
        {
            PagedResult<SecurityActivityItemResponse> activity =
                await _identityAccountClient.GetSecurityActivityPageAsync(
                    pageNumber: pageNumber,
                    pageSize: pageSize,
                    cancellationToken: cancellationToken);

            return Ok(activity);
        }

        [HttpPut("avatar")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ChangeAvatarResponse>> ChangeAvatar(
            IFormFile? avatar,
            CancellationToken cancellationToken)
        {
            if (avatar is null || avatar.Length == 0)
            {
                return ApiProblemDetailsFactory.CreateObjectResult(
                    context: HttpContext,
                    statusCode: StatusCodes.Status400BadRequest,
                    code: "Gateway.EmptyAvatar",
                    message: "Avatar file is required.");
            }

            ChangeAvatarResponse dto =
                await _identityAccountClient.ChangeAvatarAsync(
                    avatar: avatar,
                    cancellationToken: cancellationToken);

            dto.AvatarUrl = Request.ToPublicUrl(dto.AvatarUrl);

            return Ok(dto);
        }

        [HttpPut("display-name")]
        public async Task<ActionResult<ChangeDisplayNameResponse>> ChangeDisplayName(
            [FromBody] ChangeDisplayNameRequest request,
            CancellationToken cancellationToken)
        {
            ChangeDisplayNameResponse response =
                await _identityAccountClient.ChangeDisplayNameAsync(
                    request: request,
                    cancellationToken: cancellationToken);

            return Ok(response);
        }

        [HttpDelete("avatar")]
        public async Task<ActionResult<ChangeAvatarResponse>> ClearAvatar(CancellationToken cancellationToken)
        {
            ChangeAvatarResponse dto =
                await _identityAccountClient.ClearAvatarAsync(cancellationToken);

            dto.AvatarUrl = Request.ToPublicUrl(dto.AvatarUrl);

            return Ok(dto);
        }

        [HttpPut("username")]
        public async Task<ActionResult<ChangeUsernameResponse>> ChangeUsername(
            [FromBody] ChangeUsernameRequest request,
            CancellationToken cancellationToken)
        {
            ChangeUsernameResponse response =
                await _identityAccountClient.ChangeUsernameAsync(
                    request: request,
                    cancellationToken: cancellationToken);

            return Ok(response);
        }

        [HttpPut("email")]
        public async Task<ActionResult<ChangeEmailResponse>> ChangeEmail(
            [FromBody] ChangeEmailRequest request,
            CancellationToken cancellationToken)
        {
            ChangeEmailResponse response =
                await _identityAccountClient.ChangeEmailAsync(
                    request: request,
                    cancellationToken: cancellationToken);

            return Ok(response);
        }

        [HttpPost("email/pending/resend")]
        public async Task<IActionResult> ResendPendingEmailChange(CancellationToken cancellationToken)
        {
            await _identityAccountClient.ResendPendingEmailChangeAsync(cancellationToken);
            return NoContent();
        }

        [HttpDelete("email/pending")]
        public async Task<IActionResult> CancelPendingEmailChange(CancellationToken cancellationToken)
        {
            await _identityAccountClient.CancelPendingEmailChangeAsync(cancellationToken);
            return NoContent();
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            await _identityAccountClient.ChangePasswordAsync(
                request: request,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteAccount(
            [FromBody] DeleteAccountRequest request,
            CancellationToken cancellationToken)
        {
            await _identityAccountClient.DeleteAccountAsync(
                request: request,
                cancellationToken: cancellationToken);

            Guid? userId = TryGetCurrentUserId();
            if (userId.HasValue)
            {
                await _distributedCache.RemoveAsync(
                    key: AuthorizationCacheKeys.PermissionsVersion(userId.Value),
                    token: cancellationToken);
                await _distributedCache.RemoveAsync(
                    key: AuthorizationCacheKeys.PermissionsVersionStale(userId.Value),
                    token: cancellationToken);
            }

            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("/avatars/{fileName}")]
        public async Task<IActionResult> GetAvatar(
            [FromRoute] string fileName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.Contains("..") ||
                fileName.Contains('/') ||
                fileName.Contains('\\'))
                return BadRequest();

            using HttpResponseMessage resp = await _identityAssetsClient.GetAvatarAsync(
                fileName: fileName,
                cancellationToken: cancellationToken);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode);

            string contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            byte[] bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);

            return File(
                fileContents: bytes,
                contentType: contentType);
        }

        private Guid? TryGetCurrentUserId()
        {
            string? userIdValue =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(
                input: userIdValue,
                result: out Guid userId)
                ? userId
                : null;
        }
    }
}
