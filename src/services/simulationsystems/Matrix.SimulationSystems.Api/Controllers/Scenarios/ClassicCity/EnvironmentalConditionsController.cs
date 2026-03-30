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
                EffectiveTickId: dto.EffectiveTickId,
                FloodingIndex: dto.FloodingIndex,
                SnowAccumulationIndex: dto.SnowAccumulationIndex,
                RoadAccessibilityIndex: dto.RoadAccessibilityIndex,
                PowerCoverageIndex: dto.PowerCoverageIndex,
                UtilityContinuityIndex: dto.UtilityContinuityIndex,
                HeatingCoverageIndex: dto.HeatingCoverageIndex,
                WaterCoverageIndex: dto.WaterCoverageIndex,
                SanitationCoverageIndex: dto.SanitationCoverageIndex,
                LastEvaluatedAtUtc: dto.LastEvaluatedAtUtc,
                ResourceSupply: MapToResourceSupplyView(dto.ResourceSupply),
                Drainage: MapToSystemView(dto.Drainage),
                SnowRemoval: MapToSystemView(dto.SnowRemoval),
                RoadAccess: MapToSystemView(dto.RoadAccess),
                PowerDistribution: MapToSystemView(dto.PowerDistribution),
                UtilityIncidents: MapToSystemView(dto.UtilityIncidents),
                Heating: MapToSystemView(dto.Heating),
                WaterDistribution: MapToSystemView(dto.WaterDistribution),
                Sanitation: MapToSystemView(dto.Sanitation));
        }

        private static CityResourceSupplyConditionView MapToResourceSupplyView(CityResourceSupplyConditionDto dto)
        {
            return new CityResourceSupplyConditionView(
                SupplyStressIndex: dto.SupplyStressIndex,
                EffectiveAtUtc: dto.EffectiveAtUtc,
                Fuel: MapToResourceSupplyLineView(dto.Fuel),
                SpareParts: MapToResourceSupplyLineView(dto.SpareParts),
                Filters: MapToResourceSupplyLineView(dto.Filters),
                EmergencyWater: MapToResourceSupplyLineView(dto.EmergencyWater));
        }

        private static CityResourceSupplyLineConditionView MapToResourceSupplyLineView(CityResourceSupplyLineConditionDto dto)
        {
            return new CityResourceSupplyLineConditionView(
                StockLevelIndex: dto.StockLevelIndex,
                ResupplyReadinessIndex: dto.ResupplyReadinessIndex,
                ShortageRiskIndex: dto.ShortageRiskIndex);
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
