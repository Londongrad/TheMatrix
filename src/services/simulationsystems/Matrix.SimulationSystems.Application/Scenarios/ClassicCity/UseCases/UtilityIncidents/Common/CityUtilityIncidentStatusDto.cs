using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common
{
    public sealed record CityUtilityIncidentStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal UtilityContinuityIndex,
        decimal UtilityIncidentSupportIndex,
        bool EmergencyModeEnabled,
        decimal DispatchReadinessIndex,
        decimal RestorationCoverageIndex,
        decimal SpareCapacityIndex,
        decimal FieldCoordinationIndex,
        decimal IncidentQueuePressureIndex,
        CityUtilityIncidentSystemStatusDto System)
    {
        public static CityUtilityIncidentStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal utilityIncidentSupportIndex)
        {
            return new CityUtilityIncidentStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                UtilityContinuityIndex: state.UtilityContinuityIndex.Value,
                UtilityIncidentSupportIndex: utilityIncidentSupportIndex,
                EmergencyModeEnabled: state.UtilityIncidentInfrastructure.EmergencyModeEnabled,
                DispatchReadinessIndex: state.UtilityIncidentInfrastructure.DispatchReadinessIndex,
                RestorationCoverageIndex: state.UtilityIncidentInfrastructure.RestorationCoverageIndex,
                SpareCapacityIndex: state.UtilityIncidentInfrastructure.SpareCapacityIndex,
                FieldCoordinationIndex: state.UtilityIncidentInfrastructure.FieldCoordinationIndex,
                IncidentQueuePressureIndex: state.UtilityIncidentInfrastructure.IncidentQueuePressureIndex,
                System: CityUtilityIncidentSystemStatusDto.FromSnapshot(state.UtilityIncidents.ToSnapshot()));
        }
    }
}
