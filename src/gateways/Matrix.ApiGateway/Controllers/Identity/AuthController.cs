using System.Net;
using Matrix.ApiGateway.Contracts.Requests;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Identity.Auth;
using Matrix.BuildingBlocks.Api.Errors;
using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public sealed class AuthController(IIdentityAuthClient identityAuthClient) : ControllerBase
    {
        #region [ Fields ]

        private readonly IIdentityAuthClient _identityAuthClient = identityAuthClient;

        #endregion [ Fields ]

        #region [ Register & Login endpoints ]

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register(
            [FromBody] RegisterRequest request,
            CancellationToken ct)
        {
            RegisterResponse response = await _identityAuthClient.RegisterAsync(
                request: request,
                ct: ct);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginRequest request,
            CancellationToken ct)
        {
            LoginResponse response = await _identityAuthClient.LoginAsync(
                request: request,
                ct: ct);

            SetRefreshCookie(
                refreshToken: response.RefreshToken,
                refreshExpiresAtUtc: response.RefreshTokenExpiresAtUtc,
                isPersistent: response.IsPersistent);

            // Never expose refresh token to the frontend
            response.RefreshToken = string.Empty;

            return Ok(response);
        }

        #endregion [ Register & Login endpoints ]

        #region [ Refresh & Logout endpoints ]

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequestDto request,
            CancellationToken ct)
        {
            string? refreshToken = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                ErrorResponse error = new(
                    Code: "Auth.NoRefreshCookie",
                    Message: "No refresh token cookie.");

                return Unauthorized(error);
            }

            var downstreamRequest = new RefreshRequest
            {
                DeviceId = request.DeviceId,
                RefreshToken = refreshToken
            };

            try
            {
                LoginResponse response = await _identityAuthClient.RefreshAsync(
                    request: downstreamRequest,
                    ct: ct);

                SetRefreshCookie(
                    refreshToken: response.RefreshToken,
                    refreshExpiresAtUtc: response.RefreshTokenExpiresAtUtc,
                    isPersistent: response.IsPersistent);

                response.RefreshToken = string.Empty;

                return Ok(response);
            }
            catch (DownstreamServiceException ex) when (
                ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // Refresh token is invalid/expired/revoked -> clear cookie and rethrow.
                ClearRefreshCookie();
                throw;
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            string? refreshToken = Request.Cookies[RefreshCookieName];

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var request = new LogoutRequest
                {
                    RefreshToken = refreshToken
                };
                await _identityAuthClient.LogoutAsync(
                    request: request,
                    ct: ct);
            }

            ClearRefreshCookie();
            return NoContent();
        }

        #endregion [ Refresh & Logout endpoints ]

        #region [ Cookie Management ]

        private const string RefreshCookieName = "matrix_refresh_token";

        private void SetRefreshCookie(
            string refreshToken,
            DateTime refreshExpiresAtUtc,
            bool isPersistent)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };

            if (isPersistent)
                cookieOptions.Expires = refreshExpiresAtUtc;

            Response.Cookies.Append(
                key: RefreshCookieName,
                value: refreshToken,
                options: cookieOptions);
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(
                key: RefreshCookieName,
                options: new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
        }

        #endregion [ Cookie Management ]
    }
}
