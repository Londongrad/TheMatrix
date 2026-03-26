using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity
{
    [ApiController]
    [Authorize]
    [Route("api/classic-city/cities")]
    public sealed class EnvironmentalConditionsController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{cityId:guid}/environmental-conditions")]
        public async Task<IResult> Get(
            [FromRoute] Guid cityId,
            CancellationToken cancellationToken)
        {
            CityEnvironmentalConditionsDto? conditions = await mediator.Send(
                request: new GetCityEnvironmentalConditionsQuery(CityId: cityId),
                cancellationToken: cancellationToken);

            return conditions is null
                ? Results.NotFound()
                : Results.Ok(MapToView(conditions));
        }

        private static CityEnvironmentalConditionsView MapToView(CityEnvironmentalConditionsDto dto)
        {
            return new CityEnvironmentalConditionsView(
                CityId: dto.CityId,
                FloodingIndex: dto.FloodingIndex,
                SnowAccumulationIndex: dto.SnowAccumulationIndex,
                RoadAccessibilityIndex: dto.RoadAccessibilityIndex,
                PowerCoverageIndex: dto.PowerCoverageIndex,
                HeatingCoverageIndex: dto.HeatingCoverageIndex,
                WaterCoverageIndex: dto.WaterCoverageIndex,
                SanitationCoverageIndex: dto.SanitationCoverageIndex,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                Drainage: MapToSystemView(dto.Drainage),
                SnowRemoval: MapToSystemView(dto.SnowRemoval),
                RoadAccess: MapToSystemView(dto.RoadAccess),
                PowerDistribution: MapToSystemView(dto.PowerDistribution),
                Heating: MapToSystemView(dto.Heating),
                WaterDistribution: MapToSystemView(dto.WaterDistribution),
                Sanitation: MapToSystemView(dto.Sanitation));
        }

        private static CitySystemConditionView MapToSystemView(CitySystemConditionDto dto)
        {
            return new CitySystemConditionView(
                Kind: dto.Kind,
                LoadIndex: dto.LoadIndex,
                ServiceQualityIndex: dto.ServiceQualityIndex,
                BacklogIndex: dto.BacklogIndex,
                FailureRiskIndex: dto.FailureRiskIndex);
        }
    }
}
