using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Population.Api.Controllers.Scenarios.ClassicCity;

[ApiController]
[Authorize]
[Route(ClassicCityPopulationApiRoutes.PopulationRoute)]
public sealed class ClassicCityPopulationBootstrapController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;

    [HttpPost("init")]
    public async Task<ActionResult<CityPopulationBootstrapSummaryDto>> InitializeCityPopulation(
        [FromBody] InitializeCityPopulationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Environment);
        ArgumentNullException.ThrowIfNull(request.Tuning);

        IReadOnlyCollection<ResidentialBuildingSeedItem> residentialBuildings =
            (request.ResidentialBuildings ?? Array.Empty<ResidentialBuildingSeedDto>())
           .Select(x => new ResidentialBuildingSeedItem(
                ResidentialBuildingId: x.ResidentialBuildingId,
                DistrictId: x.DistrictId,
                ResidentCapacity: x.ResidentCapacity))
           .ToArray();
        IReadOnlyCollection<CityAnchorSeedItem> cityAnchors =
            (request.CityAnchors ?? Array.Empty<CityAnchorSeedDto>())
           .Select(x => new CityAnchorSeedItem(
                CityAnchorId: x.CityAnchorId,
                DistrictId: x.DistrictId,
                AccessRoadNodeId: x.AccessRoadNodeId,
                Name: x.Name,
                Type: x.Type,
                Capacity: x.Capacity,
                PositionX: x.PositionX,
                PositionY: x.PositionY,
                CreatedAtUtc: x.CreatedAtUtc))
           .ToArray();

        CityPopulationBootstrapSummaryDto result = await _sender.Send(
            request: new InitializeCityPopulationCommand(
                CityId: request.CityId,
                CurrentDate: request.CurrentDate,
                CreatedAtUtc: request.CreatedAtUtc,
                PeopleCount: request.PeopleCount,
                RandomSeed: request.RandomSeed,
                Environment: new CityPopulationEnvironmentInput(
                    ClimateZone: request.Environment.ClimateZone,
                    Hemisphere: request.Environment.Hemisphere,
                    UtcOffsetMinutes: request.Environment.UtcOffsetMinutes),
                Tuning: new CityPopulationBootstrapTuningInput(
                    HousingPressurePercent: request.Tuning.HousingPressurePercent,
                    EconomicStabilityPercent: request.Tuning.EconomicStabilityPercent,
                    SocialVolatilityPercent: request.Tuning.SocialVolatilityPercent,
                    FamilyFormationPercent: request.Tuning.FamilyFormationPercent),
                CityAnchors: cityAnchors,
                ResidentialBuildings: residentialBuildings),
            cancellationToken: cancellationToken);

        return Ok(result);
    }
}
