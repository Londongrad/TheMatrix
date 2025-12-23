using Matrix.ApiGateway.DownstreamClients.Identity.Sessions;
using Matrix.Identity.Contracts.Sessions.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Identity
{
    [Authorize]
    [ApiController]
    [Route("api/me/[controller]")]
    public sealed class SessionsController(IIdentitySessionsClient sessionsClient) : ControllerBase
    {
        private readonly IIdentitySessionsClient _sessionsClient = sessionsClient;

        [HttpGet]
        public async Task<ActionResult<IReadOnlyCollection<SessionResponse>>> GetSessions(CancellationToken ct)
        {
            IReadOnlyCollection<SessionResponse> sessions = await _sessionsClient.GetSessionsAsync(ct);
            return Ok(sessions);
        }

        [HttpDelete("{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
        {
            await _sessionsClient.RevokeSessionAsync(sessionId, ct);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken ct)
        {
            await _sessionsClient.RevokeAllSessionsAsync(ct);

            // Optional: if you want to clear refresh cookie on "revoke all" from Gateway side,
            // do it here as well. Otherwise Identity will invalidate refresh tokens anyway.
            Response.Cookies.Delete("matrix_refresh_token");

            return NoContent();
        }
    }
}
