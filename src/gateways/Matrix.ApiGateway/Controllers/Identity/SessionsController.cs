using Matrix.ApiGateway.DownstreamClients.Identity.Sessions;
using Matrix.Identity.Contracts.Self.Sessions.Responses;
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
        public async Task<ActionResult<IReadOnlyCollection<SessionResponse>>> GetSessions(
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<SessionResponse> sessions = await _sessionsClient.GetSessionsAsync(cancellationToken);
            return Ok(sessions);
        }

        [HttpDelete("{sessionId:guid}")]
        public async Task<IActionResult> RevokeSession(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            await _sessionsClient.RevokeSessionAsync(
                sessionId: sessionId,
                cancellationToken: cancellationToken);
            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            await _sessionsClient.RevokeAllSessionsAsync(cancellationToken);

            // Optional: if you want to clear refresh cookie on "revoke all" from Gateway side,
            // do it here as well. Otherwise Identity will invalidate refresh tokens anyway.
            Response.Cookies.Delete("matrix_refresh_token");

            return NoContent();
        }
    }
}
