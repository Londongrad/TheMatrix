using Matrix.ApiGateway.DownstreamClients.Identity.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Assets;
using Matrix.BuildingBlocks.Api.Errors;
using Matrix.Identity.Contracts.Account.Requests;
using Matrix.Identity.Contracts.Account.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AccountController(
        IIdentityAccountClient identityAccountClient,
        IIdentityAssetsClient identityAssetsClient) : ControllerBase
    {
        private readonly IIdentityAccountClient _identityAccountClient = identityAccountClient;
        private readonly IIdentityAssetsClient _identityAssetsClient = identityAssetsClient;

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile(CancellationToken cancellationToken)
        {
            UserProfileResponse profile =
                await _identityAccountClient.GetProfileAsync(cancellationToken);

            profile.AvatarUrl = ToPublicAvatarUrl(profile.AvatarUrl);

            return Ok(profile);
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
                ErrorResponse error = new(
                    Code: "Gateway.EmptyAvatar",
                    Message: "Avatar file is required.");

                return BadRequest(error);
            }

            ChangeAvatarResponse dto =
                await _identityAccountClient.ChangeAvatarAsync(
                    avatar: avatar,
                    cancellationToken: cancellationToken);

            dto.AvatarUrl = ToPublicAvatarUrl(dto.AvatarUrl);

            return Ok(dto);
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

        private string? ToPublicAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            if (Uri.TryCreate(
                    uriString: avatarUrl,
                    uriKind: UriKind.Absolute,
                    result: out _))
                return avatarUrl;

            if (!avatarUrl.StartsWith('/'))
                avatarUrl = "/" + avatarUrl;

            return $"{Request.Scheme}://{Request.Host}{avatarUrl}";
        }
    }
}
