using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.ApiGateway.Contracts.Identity.Account;
using Matrix.ApiGateway.Contracts.Identity.Auth.Requests;
using Matrix.ApiGateway.Controllers.Common;
using Matrix.ApiGateway.DownstreamClients.Identity.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AccountController(IIdentityAccountClient identityAccountClient)
        : GatewayControllerBase
    {
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            Guid? userId = GetCurrentUserId();
            if (userId is null)
            {
                ErrorResponse error = CreateError(
                    code: "Gateway.MissingUserIdClaim",
                    message: "User id claim is missing in token.");

                return Unauthorized(error);
            }

            HttpResponseMessage response =
                await identityAccountClient.GetProfileAsync(
                    userId: userId.Value,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(
                    response: response,
                    cancellationToken: cancellationToken);

            UserProfileResponseDto? profile =
                await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(cancellationToken);

            if (profile is not null)
            {
                profile.AvatarUrl = ToPublicAvatarUrl(profile.AvatarUrl);
                return Ok(profile);
            }

            {
                ErrorResponse error = CreateError(
                    code: "Gateway.InvalidIdentityResponse",
                    message: "Invalid response from Identity service.");

                return StatusCode(
                    statusCode: StatusCodes.Status500InternalServerError,
                    value: error);
            }
        }

        [HttpPut("avatar")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ChangeAvatar(
            IFormFile? avatar,
            CancellationToken cancellationToken)
        {
            if (avatar is null || avatar.Length == 0)
            {
                ErrorResponse error = CreateError(
                    code: "Gateway.EmptyAvatar",
                    message: "Avatar file is required.");

                return BadRequest(error);
            }

            Guid? userId = GetCurrentUserId();
            if (userId is null)
            {
                ErrorResponse error = CreateError(
                    code: "Gateway.MissingUserIdClaim",
                    message: "User id claim is missing in token.");

                return Unauthorized(error);
            }

            HttpResponseMessage response = await identityAccountClient
               .ChangeAvatarAsync(
                    userId: userId.Value,
                    avatar: avatar,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                // ошибки просто проксируем как есть
                return await ProxyDownstreamErrorAsync(
                    response: response,
                    cancellationToken: cancellationToken);

            ChangeAvatarResponseDto? dto =
                await response.Content.ReadFromJsonAsync<ChangeAvatarResponseDto>(cancellationToken: cancellationToken);

            if (dto is null || string.IsNullOrWhiteSpace(dto.AvatarUrl))
            {
                ErrorResponse error = CreateError(
                    code: "Gateway.InvalidIdentityResponse",
                    message: "Invalid response from Identity service.");

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            // ✅ Переписываем на публичный URL gateway (чтобы не было localhost:5173/avatars/...)
            dto.AvatarUrl = ToPublicAvatarUrl(dto.AvatarUrl);

            // Возвращаем нормальный JSON
            return Ok(dto);
        }

        [AllowAnonymous]
        [HttpGet("/avatars/{fileName}")]
        public async Task<IActionResult> GetAvatar(
            [FromRoute] string fileName,
            [FromServices] IIdentityAssetsClient identityAssetsClient,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.Contains("..") ||
                fileName.Contains('/') ||
                fileName.Contains('\\'))
                return BadRequest();

            using HttpResponseMessage resp = await identityAssetsClient.GetAvatarAsync(fileName, ct);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode);

            string contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            byte[] bytes = await resp.Content.ReadAsByteArrayAsync(ct);

            return File(bytes, contentType);
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequestDto requestDto,
            CancellationToken cancellationToken)
        {
            Guid? userId = GetCurrentUserId();
            if (userId is null)
            {
                ErrorResponse error = CreateError(
                    code: "Gateway.MissingUserIdClaim",
                    message: "User id claim is missing in token.");

                return Unauthorized(error);
            }

            var request = new ChangePasswordRequest
            {
                CurrentPassword = requestDto.CurrentPassword,
                NewPassword = requestDto.NewPassword
            };

            HttpResponseMessage response = await identityAccountClient
               .ChangePasswordAsync(
                    userId: userId.Value,
                    request: request,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(
                    response: response,
                    cancellationToken: cancellationToken);

            return NoContent();
        }

        private Guid? GetCurrentUserId()
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null)
                return null;

            return Guid.TryParse(
                input: userIdClaim.Value,
                result: out Guid userId)
                ? userId
                : null;
        }

        private string? ToPublicAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
                return null;

            if (Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
                return avatarUrl;

            if (!avatarUrl.StartsWith('/'))
                avatarUrl = "/" + avatarUrl;

            return $"{Request.Scheme}://{Request.Host}{avatarUrl}";
        }
    }
}
