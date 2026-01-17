using Matrix.CityCore.Contracts.Requests;
using Matrix.CityCore.Infrastructure.Public;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.CityCore.Api.Controllers
{
    [ApiController]
    [Route("api/cities/{cityId:guid}")]
    public sealed class CityClockController(ICityCoreClockAppService service) : ControllerBase
    {
        [HttpPost("bootstrap")]
        public async Task<IResult> Bootstrap(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await service.BootstrapAsync(
                cityId: cityId,
                startSimTimeUtc: DateTimeOffset.UtcNow,
                cancellationToken: cancellationToken);

            return Results.Ok();
        }

        [HttpGet("clock")]
        public async Task<IResult> GetClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityClockView? clock = await service.GetClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return clock is null
                ? Results.NotFound()
                : Results.Ok(clock);
        }

        [HttpPost("clock/pause")]
        public async Task<IResult> Pause(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            bool updated = await service.PauseAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("clock/resume")]
        public async Task<IResult> Resume(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            bool updated = await service.ResumeAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("clock/speed")]
        public async Task<IResult> SetSpeed(
            [FromRoute] Guid cityId,
            [FromBody] SetSpeedRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await service.SetSpeedAsync(
                cityId: cityId,
                multiplier: request.Multiplier,
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }

        [HttpPost("clock/jump")]
        public async Task<IResult> Jump(
            [FromRoute] Guid cityId,
            [FromBody] JumpClockRequest request,
            CancellationToken cancellationToken)
        {
            bool updated = await service.JumpAsync(
                cityId: cityId,
                newSimTimeUtc: request.NewSimTimeUtc,
                cancellationToken: cancellationToken);

            return updated
                ? Results.Ok()
                : Results.NotFound();
        }
    }
}
