using System.Security.Claims;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions;
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
        public async Task<ActionResult<List<SessionResponse>>> GetSessions(CancellationToken cancellationToken)
        {
            var query = new GetMySessionsQuery();
            Guid? currentSessionId = TryGetCurrentSessionId(User);

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
                    RefreshTokenExpiresAtUtc = s.RefreshTokenExpiresAtUtc,
                    IsActive = s.IsActive,
                    IsCurrent = currentSessionId.HasValue && s.Id == currentSessionId.Value,
                    IsPersistent = s.IsPersistent
                })
               .ToList();

            return Ok(response);
        }

        [HttpGet("history")]
        public async Task<ActionResult<PagedResult<SessionResponse>>> GetSessionHistoryPage(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var pagination = new Pagination(
                pageNumber: pageNumber,
                pageSize: pageSize);

            var query = new GetMySessionHistoryPageQuery(pagination);

            PagedResult<MySessionResult> result = await _sender.Send(
                request: query,
                cancellationToken: cancellationToken);

            var mapped = new PagedResult<SessionResponse>(
                items: result.Items.Select(s => new SessionResponse
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
                        RefreshTokenExpiresAtUtc = s.RefreshTokenExpiresAtUtc,
                        IsActive = false,
                        IsCurrent = false,
                        IsPersistent = s.IsPersistent
                    })
                   .ToList(),
                totalCount: result.TotalCount,
                pageNumber: result.PageNumber,
                pageSize: result.PageSize);

            return Ok(mapped);
        }

        [HttpDelete("{sessionId:guid}")]
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
        public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
        {
            var command = new RevokeAllMySessionsCommand();

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            // Idempotent: даже если все токены уже были отозваны, просто возвращаем 204
            return NoContent();
        }

        [HttpDelete("others")]
        public async Task<IActionResult> RevokeOtherSessions(CancellationToken cancellationToken)
        {
            var command = new RevokeOtherMySessionsCommand();

            await _sender.Send(
                request: command,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        private static Guid? TryGetCurrentSessionId(ClaimsPrincipal? user)
        {
            string? sessionId = user?.FindFirstValue(JwtClaimNames.SessionId) ?? user?.FindFirstValue(ClaimTypes.Sid);

            return Guid.TryParse(
                input: sessionId,
                result: out Guid parsedSessionId)
                ? parsedSessionId
                : null;
        }
    }
}
