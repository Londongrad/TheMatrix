using Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Services.CityCore.Scenarios.ClassicCity.SetupSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.CityCore.Scenarios.ClassicCity.SetupSessions
{
    [Authorize]
    [ApiController]
    [Route("api/scenarios/classic-city/setup-sessions")]
    public sealed class ClassicCitySetupSessionsController(
        IClassicCitySetupSessionService setupSessionService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<ClassicCitySetupSessionView>> Create(
            [FromBody] CreateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionView session = await setupSessionService.CreateAsync(
                request: request,
                cancellationToken: cancellationToken);

            return Created(
                uri: $"/api/scenarios/classic-city/setup-sessions/{session.SessionId}",
                value: session);
        }

        [HttpGet("{sessionId:guid}")]
        public async Task<ActionResult<ClassicCitySetupSessionView>> Get(
            [FromRoute] Guid sessionId,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionView? session = await setupSessionService.GetAsync(
                sessionId: sessionId,
                cancellationToken: cancellationToken);

            return session is null
                ? NotFound()
                : Ok(session);
        }

        [HttpPut("{sessionId:guid}")]
        public async Task<ActionResult<ClassicCitySetupSessionView>> Update(
            [FromRoute] Guid sessionId,
            [FromBody] UpdateClassicCitySetupSessionRequestDto request,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionMutationResult result = await setupSessionService.UpdateAsync(
                sessionId: sessionId,
                request: request,
                cancellationToken: cancellationToken);

            return result.Status switch
            {
                ClassicCitySetupSessionMutationStatus.Updated => Ok(result.Session),
                ClassicCitySetupSessionMutationStatus.NotFound => NotFound(),
                ClassicCitySetupSessionMutationStatus.Conflict => Conflict(new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                }),
                ClassicCitySetupSessionMutationStatus.Invalid => BadRequest(new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpPost("{sessionId:guid}/launch")]
        public async Task<ActionResult<ClassicCitySetupSessionView>> Launch(
            [FromRoute] Guid sessionId,
            CancellationToken cancellationToken)
        {
            ClassicCitySetupSessionMutationResult result = await setupSessionService.QueueLaunchAsync(
                sessionId: sessionId,
                cancellationToken: cancellationToken);

            return result.Status switch
            {
                ClassicCitySetupSessionMutationStatus.Updated => Accepted(result.Session),
                ClassicCitySetupSessionMutationStatus.NotFound => NotFound(),
                ClassicCitySetupSessionMutationStatus.Conflict => Conflict(new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                }),
                ClassicCitySetupSessionMutationStatus.Invalid => BadRequest(new
                {
                    code = result.ErrorCode,
                    message = result.ErrorMessage
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }
    }
}
