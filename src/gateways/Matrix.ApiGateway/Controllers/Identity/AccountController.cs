using Matrix.ApiGateway.DownstreamClients.Identity.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [ApiController]
    [Route("api/account")]
    [Authorize]
    public sealed class AccountController(IIdentityAccountClient identityAccountClient)
        : ControllerBase
    {
        private readonly IIdentityAccountClient _identityAccountClient = identityAccountClient;

        private static async Task<ContentResult> ProxyDownstreamErrorAsync(
            HttpResponseMessage response,
            CancellationToken ct)
        {
            string body = await response.Content.ReadAsStringAsync(ct);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString()
                              ?? "application/json"
            };
        }

        private string? GetAuthorizationHeader()
        {
            string auth = Request.Headers.Authorization.ToString();
            return string.IsNullOrWhiteSpace(auth) ? null : auth;
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> ChangeAvatar(
            [FromBody] ChangeAvatarRequest request,
            CancellationToken ct)
        {
            string? authorization = GetAuthorizationHeader();
            if (authorization is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.MissingAuthorizationHeader",
                    Message: "Authorization header is required.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return Unauthorized(error);
            }

            HttpResponseMessage response = await _identityAccountClient
                .ChangeAvatarAsync(authorizationHeader: authorization, request: request, ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            return NoContent();
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordRequest request,
            CancellationToken ct)
        {
            string? authorization = GetAuthorizationHeader();
            if (authorization is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.MissingAuthorizationHeader",
                    Message: "Authorization header is required.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return Unauthorized(error);
            }

            HttpResponseMessage response = await _identityAccountClient
                .ChangePasswordAsync(authorizationHeader: authorization, request: request, ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            return NoContent();
        }
    }
}
