using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Matrix.ApiGateway.Contracts.Identity.Account;
using Matrix.ApiGateway.Contracts.Identity.Auth.Requests;
using Matrix.ApiGateway.Controllers.Common;
using Matrix.ApiGateway.DownstreamClients.Identity.Account;
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
        private readonly IIdentityAccountClient _identityAccountClient = identityAccountClient;

        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
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
                await _identityAccountClient.GetProfileAsync(
                    userId: userId.Value,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(
                    response: response,
                    cancellationToken: cancellationToken);

            UserProfileResponseDto? profile =
                await response.Content.ReadFromJsonAsync<UserProfileResponseDto>(cancellationToken);

            if (profile is not null)
                return Ok(profile);

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

            HttpResponseMessage response = await _identityAccountClient
               .ChangeAvatarAsync(
                    userId: userId.Value,
                    avatar: avatar,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                // ошибки просто проксируем как есть
                return await ProxyDownstreamErrorAsync(
                    response: response,
                    cancellationToken: cancellationToken);

            // ✅ Успех: прокидываем тело ответа Identity дальше на фронт
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
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

            HttpResponseMessage response = await _identityAccountClient
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
    }
}
