using Matrix.Identity.Api.Contracts.Responses;
using Matrix.Identity.Application.UseCases.Sessions.GetMySessions;
using Matrix.Identity.Application.UseCases.Sessions.RevokeAllMySessions;
using Matrix.Identity.Application.UseCases.Sessions.RevokeMySession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Identity.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MySessionsController(ISender sender) : ControllerBase
    {
        private readonly ISender _sender = sender;

        [HttpGet("sessions")]
        public async Task<ActionResult<List<SessionResponse>>> GetSessions(
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
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

        [HttpDelete("sessions/{sessionId:guid}")]
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

        [HttpDelete("sessions")]
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
