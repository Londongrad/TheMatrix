using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Matrix.ApiGateway.DownstreamClients.Identity.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController(IIdentityAuthClient identityApiClient) : ControllerBase
    {
        private readonly IIdentityAuthClient _identityApiClient = identityApiClient;

        #region [ Proxy Downstream Errors ]

        private static async Task<ContentResult> ProxyDownstreamErrorAsync(
            HttpResponseMessage response,
            CancellationToken ct)
        {
            string body = await response.Content.ReadAsStringAsync(ct);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }

        #endregion [ Proxy Downstream Errors ]

        #region [ Cookie Management ]

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

            Response.Cookies.Append(key: RefreshCookieName, value: refreshToken, options: cookieOptions);
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(key: RefreshCookieName, options: new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
        }

        #endregion [ Cookie Management ]

        #region [ Registration and Login ]

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            HttpResponseMessage response = await _identityApiClient.RegisterAsync(request: request, ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            RegisterResponse? registerResponse =
                await response.Content.ReadFromJsonAsync<RegisterResponse>(cancellationToken: ct);

            if (registerResponse is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
            }

            return Ok(registerResponse);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            HttpResponseMessage response = await _identityApiClient.LoginAsync(request: request, ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            LoginResponse? loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (loginResponse is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
            }

            SetRefreshCookie(refreshToken: loginResponse.RefreshToken,
                refreshExpiresAtUtc: loginResponse.RefreshTokenExpiresAtUtc);
            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        #endregion [ Registration and Login ]

        #region [ Token Refresh and Logout ]

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken ct)
        {
            string? refreshToken = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                var error = new ErrorResponse(
                    Code: "Auth.NoRefreshCookie",
                    Message: "No refresh token cookie.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return Unauthorized(error);
            }

            // Куки контролируем мы, поэтому перезаписываем, даже если клиент что-то прислал
            request.RefreshToken = refreshToken;

            HttpResponseMessage response = await _identityApiClient.RefreshAsync(request: request, ct: ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    ClearRefreshCookie();

                return await ProxyDownstreamErrorAsync(response: response, ct: ct);
            }

            LoginResponse? loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (loginResponse is null)
            {
                ClearRefreshCookie();

                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
            }

            // успешный refresh → ротация куки
            SetRefreshCookie(refreshToken: loginResponse.RefreshToken,
                refreshExpiresAtUtc: loginResponse.RefreshTokenExpiresAtUtc);
            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            string? refreshToken = Request.Cookies[RefreshCookieName];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var request = new RefreshRequest { RefreshToken = refreshToken };

                await _identityApiClient.LogoutAsync(request: request, ct: ct);
            }

            ClearRefreshCookie();
            return NoContent();
        }

        #endregion [ Token Refresh and Logout ]

        #region [ Sessions Management ]

        [Authorize]
        [HttpGet("me")]
        public ActionResult<MeResponse> Me()
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            Claim? emailClaim =
                User.FindFirst(JwtRegisteredClaimNames.Email) ??
                User.FindFirst(ClaimTypes.Email);

            Claim? usernameClaim =
                User.FindFirst(JwtRegisteredClaimNames.UniqueName) ??
                User.FindFirst(ClaimTypes.Name);

            if (userIdClaim is null || emailClaim is null || usernameClaim is null) return Unauthorized();

            if (!Guid.TryParse(input: userIdClaim.Value, result: out Guid userId)) return Unauthorized();

            var response = new MeResponse
            {
                UserId = userId,
                Email = emailClaim.Value,
                Username = usernameClaim.Value
            };

            return Ok(response);
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(CancellationToken ct)
        {
            string authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization)) return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.GetSessionsAsync(authorizationHeader: authorization, ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            List<SessionResponse>? sessions =
                await response.Content.ReadFromJsonAsync<List<SessionResponse>>(cancellationToken: ct);
            if (sessions is null)
            {
                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

                return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
            }

            return Ok(sessions);
        }

        [Authorize]
        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
        {
            string authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization)) return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.RevokeSessionAsync(authorizationHeader: authorization, sessionId: sessionId,
                    ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            return NoContent();
        }

        [Authorize]
        [HttpDelete("sessions")]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
        {
            string authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization)) return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.RevokeAllSessionsAsync(authorizationHeader: authorization, ct: ct);

            if (!response.IsSuccessStatusCode) return await ProxyDownstreamErrorAsync(response: response, ct: ct);

            // Мы только что убили все refresh-токены, чистим куку
            ClearRefreshCookie();

            return NoContent();
        }

        #endregion [ Sessions Management ]
    }
}
