using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Matrix.ApiGateway.Contracts.Identity.Auth.Requests;
using Matrix.ApiGateway.Controllers.Common;
using Matrix.ApiGateway.DownstreamClients.Identity.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;
using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Responses;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController(IIdentityAuthClient identityApiClient) : GatewayControllerBase
    {
        private readonly IIdentityAuthClient _identityApiClient = identityApiClient;

        #region [ Cookie Management ]

        private const string RefreshCookieName = "matrix_refresh_token";

        private void SetRefreshCookie(string refreshToken, DateTime refreshExpiresAtUtc, bool isPersistent)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };

            if (isPersistent)
                // persistent cookie – живёт до RefreshTokenExpiresAtUtc
                cookieOptions.Expires = refreshExpiresAtUtc;
            // иначе: НИЧЕГО не ставим → сессионная кука (пропадёт при закрытии браузера)

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

        #region [ Registration and Login endpoints ]

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request,
            CancellationToken cancellationToken)
        {
            var registerRequest = new RegisterRequest
            {
                Email = request.Email,
                Username = request.Username,
                Password = request.Password
            };

            HttpResponseMessage response =
                await _identityApiClient.RegisterAsync(request: registerRequest, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            RegisterResponse? registerResponse =
                await response.Content.ReadFromJsonAsync<RegisterResponse>(cancellationToken);

            if (registerResponse is not null) return Ok(registerResponse);

            ErrorResponse error = CreateError(
                code: "Gateway.InvalidIdentityResponse",
                message: "Invalid response from Identity service.");

            return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            var loginRequest = new LoginRequest
            {
                Login = request.Login,
                Password = request.Password,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                RememberMe = request.RememberMe
            };

            string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = Request.Headers.UserAgent.ToString();

            HttpResponseMessage response =
                await _identityApiClient.LoginAsync(
                    request: loginRequest,
                    clientIp: clientIp,
                    userAgent: userAgent,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            LoginResponse? loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);

            if (loginResponse is null)
            {
                ErrorResponse error = CreateError(
                    code: "Gateway.InvalidIdentityResponse",
                    message: "Invalid response from Identity service.");

                return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
            }

            SetRefreshCookie(
                refreshToken: loginResponse.RefreshToken,
                refreshExpiresAtUtc: loginResponse.RefreshTokenExpiresAtUtc,
                isPersistent: loginResponse.IsPersistent);

            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        #endregion [ Registration and Login endpoints ]

        #region [ Token Refresh and Logout endpoints ]

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshRequestDto requestDto,
            CancellationToken cancellationToken)
        {
            string? refreshToken = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                ErrorResponse error = CreateError(
                    code: "Auth.NoRefreshCookie",
                    message: "No refresh token cookie.");

                return Unauthorized(error);
            }

            string? clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = Request.Headers.UserAgent.ToString();

            var refreshRequest = new RefreshRequest { DeviceId = requestDto.DeviceId, RefreshToken = refreshToken };

            HttpResponseMessage response =
                await _identityApiClient.RefreshAsync(
                    request: refreshRequest,
                    clientIp: clientIp,
                    userAgent: userAgent,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    ClearRefreshCookie();

                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);
            }

            LoginResponse? loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);

            if (loginResponse is null)
            {
                ClearRefreshCookie();

                ErrorResponse error = CreateError(
                    code: "Gateway.InvalidIdentityResponse",
                    message: "Invalid response from Identity service.");

                return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
            }

            // успешный refresh → ротация куки
            SetRefreshCookie(
                refreshToken: loginResponse.RefreshToken,
                refreshExpiresAtUtc: loginResponse.RefreshTokenExpiresAtUtc,
                isPersistent: loginResponse.IsPersistent);

            loginResponse.RefreshToken = string.Empty;

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            string? refreshToken = Request.Cookies[RefreshCookieName];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var request = new LogoutRequest { RefreshToken = refreshToken };

                await _identityApiClient.LogoutAsync(request: request, cancellationToken: cancellationToken);
            }

            ClearRefreshCookie();
            return NoContent();
        }

        #endregion [ Token Refresh and Logout endpoints ]

        #region [ Sessions Management endpoints ]

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        {
            // userId только из клеймов (Gateway – граница)
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.GetSessionsAsync(userId: userId, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            List<SessionResponse>? sessions =
                await response.Content.ReadFromJsonAsync<List<SessionResponse>>(cancellationToken);

            if (sessions is not null) return Ok(sessions);

            ErrorResponse error = CreateError(
                code: "Gateway.InvalidIdentityResponse",
                message: "Invalid response from Identity service.");

            return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
        }

        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.RevokeSessionAsync(userId: userId, sessionId: sessionId,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpDelete("sessions")]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            Claim? userIdClaim =
                User.FindFirst(JwtRegisteredClaimNames.Sub) ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.RevokeAllSessionsAsync(userId: userId,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            // Мы только что убили все refresh-токены, чистим куку
            ClearRefreshCookie();

            return NoContent();
        }

        #endregion [ Sessions Management endpoints ]
    }
}
