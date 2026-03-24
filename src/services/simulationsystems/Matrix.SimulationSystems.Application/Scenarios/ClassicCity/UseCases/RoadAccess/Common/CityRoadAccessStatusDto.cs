using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common
{
    public sealed record CityRoadAccessStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal FloodingIndex,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal RoadSupportIndex,
        bool EmergencyModeEnabled,
        decimal CorridorAvailabilityIndex,
        decimal SurfaceIntegrityIndex,
        decimal TrafficControlReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityRoadAccessSystemStatusDto System)
    {
        public static CityRoadAccessStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal roadSupportIndex)
        {
            return new CityRoadAccessStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                FloodingIndex: state.FloodingIndex.Value,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                RoadSupportIndex: roadSupportIndex,
                EmergencyModeEnabled: state.RoadAccessInfrastructure.EmergencyModeEnabled,
                CorridorAvailabilityIndex: state.RoadAccessInfrastructure.CorridorAvailabilityIndex,
                SurfaceIntegrityIndex: state.RoadAccessInfrastructure.SurfaceIntegrityIndex,
                TrafficControlReadinessIndex: state.RoadAccessInfrastructure.TrafficControlReadinessIndex,
                CrewReadinessIndex: state.RoadAccessInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.RoadAccessInfrastructure.IncidentPressureIndex,
                System: CityRoadAccessSystemStatusDto.FromSnapshot(state.RoadAccess.ToSnapshot()));
        }
    }
}
