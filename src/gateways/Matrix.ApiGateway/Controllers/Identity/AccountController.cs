using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        : ControllerBase
    {
        private readonly IIdentityAccountClient _identityAccountClient = identityAccountClient;

        private static async Task<ContentResult> ProxyDownstreamErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString()
                              ?? "application/json"
            };
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
                var error = new ErrorResponse(
                    Code: "Gateway.EmptyAvatar",
                    Message: "Avatar file is required.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return BadRequest(error);
            }

            Guid? userId = GetCurrentUserId();
            if (userId is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.MissingUserIdClaim",
                    Message: "User id claim is missing in token.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return Unauthorized(error);
            }

            HttpResponseMessage response = await _identityAccountClient
                .ChangeAvatarAsync(userId: userId.Value, avatar: avatar, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                // ошибки просто проксируем как есть
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            // ✅ Успех: прокидываем тело ответа Identity дальше на фронт
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString()
                              ?? "application/json"
            };
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken cancellationToken)
        {
            Guid? userId = GetCurrentUserId();
            if (userId is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.MissingUserIdClaim",
                    Message: "User id claim is missing in token.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return Unauthorized(error);
            }

            HttpResponseMessage response = await _identityAccountClient
                .ChangePasswordAsync(userId: userId.Value, request: request, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

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
