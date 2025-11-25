using Matrix.ApiGateway.DownstreamClients.Identity;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var response = await _identityApiClient.RegisterAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                return StatusCode((int)response.StatusCode, body);
            }

            var registerResponse =
            await response.Content.ReadFromJsonAsync<RegisterResponse>(ct);

            if (registerResponse is null)
            {
                return StatusCode(500, "Invalid response from Identity service");
            }

            return Ok(registerResponse);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            var loginResponse = await _identityApiClient.LoginAsync(request, ct);

            if (loginResponse is null)
            {
                return Unauthorized(new { error = "Invalid credentials or Identity service error." });
            }

            SetRefreshCookie(loginResponse.RefreshToken, loginResponse.RefreshTokenExpiresAtUtc);

            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized("No refresh token cookie");
            }

            var request = new RefreshRequest { RefreshToken = refreshToken };

            var result = await _identityApiClient.RefreshAsync(request, ct);

            if (result is null)
            {
                ClearRefreshCookie();
                return Unauthorized("Invalid refresh token");
            }

            // Обновляем cookie новым refresh-токеном (ротация)
            SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            result.RefreshToken = string.Empty;
            return Ok(result);
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