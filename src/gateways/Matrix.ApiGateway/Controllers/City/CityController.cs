using Matrix.ApiGateway.Contracts.City.Requests;
using Matrix.ApiGateway.Contracts.City.Responses;
using Matrix.ApiGateway.Controllers.City.Mappers;
using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.CityCore.Models;
using Matrix.CityCore.Contracts.Simulation.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.City
{
    [Authorize]
    [ApiController]
    [Route("api/city")]
    public sealed class CityController(ICityCoreApiClient cityCoreClient) : ControllerBase
    {
        private readonly ICityCoreApiClient _cityCoreClient = cityCoreClient;

        [HttpPost("bootstrap")]
        public async Task<ActionResult<BootstrapCityResponseDto>> Bootstrap(CancellationToken cancellationToken)
        {
            BootstrapCityResponseDto response = await _cityCoreClient.BootstrapAsync(
                cancellationToken: cancellationToken);

            return Ok(
                new BootstrapCityResponseDto
                {
                    CityId = response.CityId
                });
        }

        [HttpGet("{cityId:guid}/clock")]
        public async Task<ActionResult<CityClockResponseDto>> GetClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityCoreClockResponseDto clock = await _cityCoreClient.GetClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(clock.ToBffResponse());
        }

        [HttpPost("{cityId:guid}/clock/pause")]
        public async Task<IActionResult> PauseClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.PauseClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{cityId:guid}/clock/resume")]
        public async Task<IActionResult> ResumeClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.ResumeClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{cityId:guid}/clock/speed")]
        public async Task<IActionResult> SetClockSpeed(
            [FromRoute] Guid cityId,
            [FromBody] SetCityClockSpeedRequestDto request,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.SetClockSpeedAsync(
                cityId: cityId,
                request: new SetSpeedRequest(request.Multiplier),
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("{cityId:guid}/clock/jump")]
        public async Task<IActionResult> JumpClock(
            [FromRoute] Guid cityId,
            [FromBody] JumpCityClockRequestDto request,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.JumpClockAsync(
                cityId: cityId,
                request: new JumpClockRequest(request.NewSimTimeUtc),
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
