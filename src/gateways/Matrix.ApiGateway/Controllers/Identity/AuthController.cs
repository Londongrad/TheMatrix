using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Matrix.ApiGateway.Contracts.Identity.Auth.Requests;
using Matrix.ApiGateway.Contracts.Identity.Auth.Responses;
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
    public sealed class AuthController(IIdentityAuthClient identityApiClient) : ControllerBase
    {
        private readonly IIdentityAuthClient _identityApiClient = identityApiClient;

        #region [ Proxy Downstream Errors ]

        private static async Task<ContentResult> ProxyDownstreamErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

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

            var error = new ErrorResponse(
                Code: "Gateway.InvalidIdentityResponse",
                Message: "Invalid response from Identity service.",
                Errors: null,
                TraceId: HttpContext.TraceIdentifier);

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
                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

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
                var error = new ErrorResponse(
                    Code: "Auth.NoRefreshCookie",
                    Message: "No refresh token cookie.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

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

                var error = new ErrorResponse(
                    Code: "Gateway.InvalidIdentityResponse",
                    Message: "Invalid response from Identity service.",
                    Errors: null,
                    TraceId: HttpContext.TraceIdentifier);

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

            if (userIdClaim is null
                || emailClaim is null
                || usernameClaim is null
                || !Guid.TryParse(input: userIdClaim.Value, result: out Guid userId))
                return Unauthorized();

            var response = new MeResponseDto
            {
                UserId = userId,
                Email = emailClaim.Value,
                Username = usernameClaim.Value
            };

            return Ok(response);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        {
            string authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization)) return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.GetSessionsAsync(authorizationHeader: authorization,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            List<SessionResponse>? sessions =
                await response.Content.ReadFromJsonAsync<List<SessionResponse>>(cancellationToken);

            if (sessions is not null) return Ok(sessions);

            var error = new ErrorResponse(
                Code: "Gateway.InvalidIdentityResponse",
                Message: "Invalid response from Identity service.",
                Errors: null,
                TraceId: HttpContext.TraceIdentifier);

            return StatusCode(statusCode: StatusCodes.Status500InternalServerError, value: error);
        }

        [HttpDelete("sessions/{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
        {
            string authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization)) return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.RevokeSessionAsync(authorizationHeader: authorization, sessionId: sessionId,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ProxyDownstreamErrorAsync(response: response, cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpDelete("sessions")]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            string authorization = Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorization)) return Unauthorized();

            HttpResponseMessage response =
                await _identityApiClient.RevokeAllSessionsAsync(authorizationHeader: authorization,
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
