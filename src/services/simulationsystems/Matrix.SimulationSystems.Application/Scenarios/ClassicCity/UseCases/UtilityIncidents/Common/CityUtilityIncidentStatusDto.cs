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
        string? BudgetAuthorizationStatus,
        string? BudgetAuthorizationLevel,
        decimal? BudgetAvailableAmount,
        bool? BudgetAuthorizedByEmergencyOverride,
        string? BudgetAuthorizedIntensity,
        string? BudgetAuthorizationSummary,
        CityUtilityIncidentSystemStatusDto System)
    {
        public static CityUtilityIncidentStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal utilityIncidentSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null,
            string? budgetAuthorizationStatus = null,
            string? budgetAuthorizationLevel = null,
            decimal? budgetAvailableAmount = null,
            bool? budgetAuthorizedByEmergencyOverride = null,
            string? budgetAuthorizedIntensity = null,
            string? budgetAuthorizationSummary = null)
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
                BudgetAuthorizationStatus: budgetAuthorizationStatus,
                BudgetAuthorizationLevel: budgetAuthorizationLevel,
                BudgetAvailableAmount: budgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: budgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: budgetAuthorizedIntensity,
                BudgetAuthorizationSummary: budgetAuthorizationSummary,
                System: CityUtilityIncidentSystemStatusDto.FromSnapshot(state.UtilityIncidents.ToSnapshot()));
        }
    }
}
