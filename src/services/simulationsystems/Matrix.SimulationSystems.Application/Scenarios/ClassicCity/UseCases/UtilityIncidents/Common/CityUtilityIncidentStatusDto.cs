using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common
{
    public sealed record CityUtilityIncidentStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal UtilityContinuityIndex,
        decimal UtilityIncidentSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal DispatchReadinessIndex,
        decimal RestorationCoverageIndex,
        decimal SpareCapacityIndex,
        decimal FieldCoordinationIndex,
        decimal IncidentQueuePressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        CityUtilityIncidentSystemStatusDto System)
    {
        public static CityUtilityIncidentStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal utilityIncidentSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null)
        {
            return new CityUtilityIncidentStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                UtilityContinuityIndex: state.UtilityContinuityIndex.Value,
                UtilityIncidentSupportIndex: utilityIncidentSupportIndex,
                BudgetPressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EmergencyModeEnabled: state.UtilityIncidentInfrastructure.EmergencyModeEnabled,
                DispatchReadinessIndex: state.UtilityIncidentInfrastructure.DispatchReadinessIndex,
                RestorationCoverageIndex: state.UtilityIncidentInfrastructure.RestorationCoverageIndex,
                SpareCapacityIndex: state.UtilityIncidentInfrastructure.SpareCapacityIndex,
                FieldCoordinationIndex: state.UtilityIncidentInfrastructure.FieldCoordinationIndex,
                IncidentQueuePressureIndex: state.UtilityIncidentInfrastructure.IncidentQueuePressureIndex,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: appliedIntensity,
                System: CityUtilityIncidentSystemStatusDto.FromSnapshot(state.UtilityIncidents.ToSnapshot()));
        }
    }
}
