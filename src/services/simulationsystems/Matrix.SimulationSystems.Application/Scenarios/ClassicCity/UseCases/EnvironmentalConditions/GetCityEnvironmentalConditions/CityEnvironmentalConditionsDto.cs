using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions
{
    public sealed record CityEnvironmentalConditionsDto(
        Guid CityId,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal PowerCoverageIndex,
        decimal HeatingCoverageIndex,
        decimal WaterCoverageIndex,
        decimal SanitationCoverageIndex,
        DateTimeOffset LastEvaluatedAtUtc,
        CitySystemConditionDto Drainage,
        CitySystemConditionDto SnowRemoval,
        CitySystemConditionDto RoadAccess,
        CitySystemConditionDto PowerDistribution,
        CitySystemConditionDto Heating,
        CitySystemConditionDto WaterDistribution,
        CitySystemConditionDto Sanitation)
    {
        public static CityEnvironmentalConditionsDto FromDomain(CityEnvironmentalConditionState state)
        {
            return new CityEnvironmentalConditionsDto(
                CityId: state.SimulationHostId.Value,
                FloodingIndex: state.FloodingIndex.Value,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                PowerCoverageIndex: state.PowerCoverageIndex.Value,
                HeatingCoverageIndex: state.HeatingCoverageIndex.Value,
                WaterCoverageIndex: state.WaterCoverageIndex.Value,
                SanitationCoverageIndex: state.SanitationCoverageIndex.Value,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                Drainage: CitySystemConditionDto.FromSnapshot(state.Drainage.ToSnapshot()),
                SnowRemoval: CitySystemConditionDto.FromSnapshot(state.SnowRemoval.ToSnapshot()),
                RoadAccess: CitySystemConditionDto.FromSnapshot(state.RoadAccess.ToSnapshot()),
                PowerDistribution: CitySystemConditionDto.FromSnapshot(state.PowerDistribution.ToSnapshot()),
                Heating: CitySystemConditionDto.FromSnapshot(state.Heating.ToSnapshot()),
                WaterDistribution: CitySystemConditionDto.FromSnapshot(state.WaterDistribution.ToSnapshot()),
                Sanitation: CitySystemConditionDto.FromSnapshot(state.Sanitation.ToSnapshot()));
        }
    }
}
