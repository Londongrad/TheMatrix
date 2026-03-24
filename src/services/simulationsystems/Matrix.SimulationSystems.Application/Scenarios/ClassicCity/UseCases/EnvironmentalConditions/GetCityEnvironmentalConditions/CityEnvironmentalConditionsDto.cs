using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions
{
    public sealed record CityEnvironmentalConditionsDto(
        Guid CityId,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal HeatingCoverageIndex,
        DateTimeOffset LastEvaluatedAtUtc,
        CitySystemConditionDto Drainage,
        CitySystemConditionDto SnowRemoval,
        CitySystemConditionDto RoadAccess,
        CitySystemConditionDto Heating)
    {
        public static CityEnvironmentalConditionsDto FromDomain(CityEnvironmentalConditionState state)
        {
            return new CityEnvironmentalConditionsDto(
                CityId: state.SimulationHostId.Value,
                FloodingIndex: state.FloodingIndex.Value,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                HeatingCoverageIndex: state.HeatingCoverageIndex.Value,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                Drainage: CitySystemConditionDto.FromSnapshot(state.Drainage.ToSnapshot()),
                SnowRemoval: CitySystemConditionDto.FromSnapshot(state.SnowRemoval.ToSnapshot()),
                RoadAccess: CitySystemConditionDto.FromSnapshot(state.RoadAccess.ToSnapshot()),
                Heating: CitySystemConditionDto.FromSnapshot(state.Heating.ToSnapshot()));
        }
    }
}
