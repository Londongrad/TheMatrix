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

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var response = await _identityApiClient.RegisterAsync(request, ct);

            var content = await response.Content.ReadAsStringAsync(ct);

            return StatusCode((int)response.StatusCode, content);
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

            return Ok(loginResponse);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh(
            [FromBody] RefreshRequest request,
            CancellationToken ct)
        {
            var result = await _identityApiClient.RefreshAsync(request, ct);

            if (result is null)
            {
                return Unauthorized(new { error = "Invalid refresh token." });
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshRequest request,
            CancellationToken ct)
        {
            await _identityApiClient.LogoutAsync(request, ct);
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