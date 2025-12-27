using Matrix.BuildingBlocks.Application.Authorization;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession;
using Matrix.Identity.Contracts.Self.Sessions.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers.Self
{
    [ApiController]
    [Route("api/me/sessions")]
    [Authorize]
    public class MySessionsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        [Authorize(Policy = PermissionKeys.IdentityMeSessionsRead)]
        public async Task<ActionResult<List<SessionResponse>>> GetSessions(CancellationToken cancellationToken)
        {
            var query = new GetMySessionsQuery();

            IReadOnlyCollection<MySessionResult> sessions =
                await _sender.Send(
                    request: query,
                    cancellationToken: cancellationToken);

            var response = sessions
               .Select(s => new SessionResponse
                {
                    Id = s.Id,
                    DeviceId = s.DeviceId,
                    DeviceName = s.DeviceName,
                    UserAgent = s.UserAgent,
                    IpAddress = s.IpAddress,
                    Country = s.Country,
                    Region = s.Region,
                    City = s.City,
                    CreatedAtUtc = s.CreatedAtUtc,
                    LastUsedAtUtc = s.LastUsedAtUtc,
                    IsActive = s.IsActive
                })
               .ToList();

            return Ok(response);
        }

        [HttpDelete("{sessionId:guid}")]
        [Authorize(Policy = PermissionKeys.IdentityMeSessionsRevoke)]
        public async Task<IActionResult> RevokeSession(
            [FromRoute] Guid sessionId,
            CancellationToken cancellationToken)
        {
            var command = new RevokeMySessionCommand(SessionId: sessionId);

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            // Даже если sessionId не нашёлся – всё равно 204, запрос идемпотентный
            return NoContent();
        }

        [HttpDelete]
        [Authorize(Policy = PermissionKeys.IdentityMeSessionsRevokeAll)]
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            var command = new RevokeAllMySessionsCommand();

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            // Idempotent: даже если все токены уже были отозваны, просто возвращаем 204
            return NoContent();
        }
    }
}
