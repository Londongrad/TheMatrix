using Matrix.ApiGateway.Contracts.City.Requests;
using Matrix.ApiGateway.Contracts.City.Responses;
using Matrix.ApiGateway.Controllers.City.Mappers;
using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.CityCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.City
{
    [Authorize]
    [ApiController]
    [Route("api/city/{cityId:guid}")]
    public sealed class CityController(ICityCoreApiClient cityCoreClient) : ControllerBase
    {
        private readonly ICityCoreApiClient _cityCoreClient = cityCoreClient;

        [HttpPost("bootstrap")]
        public async Task<IActionResult> Bootstrap(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.BootstrapAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpGet("clock")]
        public async Task<ActionResult<CityClockResponseDto>> GetClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityCoreClockResponseDto clock = await _cityCoreClient.GetClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return Ok(clock.ToBffResponse());
        }

        [HttpPost("clock/pause")]
        public async Task<IActionResult> PauseClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.PauseClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("clock/resume")]
        public async Task<IActionResult> ResumeClock(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.ResumeClockAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("clock/speed")]
        public async Task<IActionResult> SetClockSpeed(
            [FromRoute] Guid cityId,
            [FromBody] SetCityClockSpeedRequestDto request,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.SetClockSpeedAsync(
                cityId: cityId,
                request: new CityCoreSetClockSpeedRequestDto
                {
                    Multiplier = request.Multiplier
                },
                cancellationToken: cancellationToken);

            return NoContent();
        }

        [HttpPost("clock/jump")]
        public async Task<IActionResult> JumpClock(
            [FromRoute] Guid cityId,
            [FromBody] JumpCityClockRequestDto request,
            CancellationToken cancellationToken)
        {
            await _cityCoreClient.JumpClockAsync(
                cityId: cityId,
                request: new CityCoreJumpClockRequestDto
                {
                    NewSimTimeUtc = request.NewSimTimeUtc
                },
                cancellationToken: cancellationToken);

            return NoContent();
        }
    }
}
