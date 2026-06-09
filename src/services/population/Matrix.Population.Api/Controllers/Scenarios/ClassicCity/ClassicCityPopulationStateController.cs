using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers.Scenarios.ClassicCity;

[ApiController]
[Authorize]
[Route(ClassicCityPopulationApiRoutes.CityRoute)]
public sealed class ClassicCityPopulationStateController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpPut("environment")]
    public async Task<IActionResult> SyncCityEnvironment(
        [FromRoute] Guid cityId,
        [FromBody] SyncCityEnvironmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _sender.Send(
            request: new SyncCityEnvironmentCommand(
                CityId: cityId,
                ClimateZone: request.ClimateZone,
                Hemisphere: request.Hemisphere,
                UtcOffsetMinutes: request.UtcOffsetMinutes),
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CityPopulationSummaryDto>> GetCitySummary(
        [FromRoute] Guid cityId,
        CancellationToken cancellationToken = default)
    {
        CityPopulationSummaryDto? result = await _sender.Send(
            request: new GetCityPopulationSummaryQuery(cityId),
            cancellationToken: cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<CityPopulationDashboardDto>> GetCityDashboard(
        [FromRoute] Guid cityId,
        CancellationToken cancellationToken = default)
    {
        CityPopulationDashboardDto? result = await _sender.Send(
            request: new GetCityDashboardQuery(cityId),
            cancellationToken: cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    [HttpGet("district-pressure")]
    public async Task<ActionResult<CityPopulationDistrictPressureDto>> GetCityDistrictPressure(
        [FromRoute] Guid cityId,
        CancellationToken cancellationToken = default)
    {
        CityPopulationDistrictPressureDto? result = await _sender.Send(
            request: new GetCityDistrictPressureQuery(cityId),
            cancellationToken: cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}
