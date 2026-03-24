using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common
{
    public sealed record CitySnowRemovalStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal SnowRemovalSupportIndex,
        bool EmergencyModeEnabled,
        decimal FleetAvailabilityIndex,
        decimal RouteCoverageIndex,
        decimal DeicingReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CitySnowRemovalSystemStatusDto System)
    {
        public static CitySnowRemovalStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal snowRemovalSupportIndex)
        {
            return new CitySnowRemovalStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                SnowRemovalSupportIndex: snowRemovalSupportIndex,
                EmergencyModeEnabled: state.SnowRemovalInfrastructure.EmergencyModeEnabled,
                FleetAvailabilityIndex: state.SnowRemovalInfrastructure.FleetAvailabilityIndex,
                RouteCoverageIndex: state.SnowRemovalInfrastructure.RouteCoverageIndex,
                DeicingReadinessIndex: state.SnowRemovalInfrastructure.DeicingReadinessIndex,
                CrewReadinessIndex: state.SnowRemovalInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.SnowRemovalInfrastructure.IncidentPressureIndex,
                System: CitySnowRemovalSystemStatusDto.FromSnapshot(state.SnowRemoval.ToSnapshot()));
        }
    }
}
