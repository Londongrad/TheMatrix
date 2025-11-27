using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Matrix.ApiGateway.DownstreamClients.Identity;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController(IIdentityApiClient identityApiClient) : ControllerBase
    {
        private readonly IIdentityApiClient _identityApiClient = identityApiClient;
        private const string RefreshCookieName = "matrix_refresh_token";

        private void SetRefreshCookie(string refreshToken, DateTime refreshExpiresAtUtc)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshExpiresAtUtc,
                Path = "/"
            };

            Response.Cookies.Append(RefreshCookieName, refreshToken, cookieOptions);
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
        }

        private static async Task<ContentResult> ProxyDownstreamErrorAsync(
            HttpResponseMessage response,
            CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var response = await _identityApiClient.RegisterAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return await ProxyDownstreamErrorAsync(response, ct);
            }

            var registerResponse = await response.Content.ReadFromJsonAsync<RegisterResponse>(cancellationToken: ct);

            if (registerResponse is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            return Ok(registerResponse);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var response = await _identityApiClient.LoginAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return await ProxyDownstreamErrorAsync(response, ct);
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (loginResponse is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            SetRefreshCookie(loginResponse.RefreshToken, loginResponse.RefreshTokenExpiresAtUtc);
            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                var error = new ErrorResponse(
                    Code: "Auth.NoRefreshCookie",
                    Message: "No refresh token cookie.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return Unauthorized(error);
            }

            var request = new RefreshRequest { RefreshToken = refreshToken };

            var response = await _identityApiClient.RefreshAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    ClearRefreshCookie();
                }

                return await ProxyDownstreamErrorAsync(response, ct);
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (loginResponse is null)
            {
                ClearRefreshCookie();

                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(StatusCodes.Status500InternalServerError, error);
            }

            // успешный refresh → ротация куки
            SetRefreshCookie(loginResponse.RefreshToken, loginResponse.RefreshTokenExpiresAtUtc);
            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var request = new RefreshRequest { RefreshToken = refreshToken };

                await _identityApiClient.LogoutAsync(request, ct);
            }

            ClearRefreshCookie();
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<MeResponse> Me()
        {
            var userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            var emailClaim =
                User.FindFirst(JwtRegisteredClaimNames.Email) ??
                User.FindFirst(ClaimTypes.Email);

            var usernameClaim =
                User.FindFirst(JwtRegisteredClaimNames.UniqueName) ??
                User.FindFirst(ClaimTypes.Name);

            if (userIdClaim is null || emailClaim is null || usernameClaim is null)
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var response = new MeResponse
            {
                UserId = userId,
                Email = emailClaim.Value,
                Username = usernameClaim.Value
            };

            return Ok(response);
        }
    }
}
