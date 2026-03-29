using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common
{
    public sealed record CityDrainageStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal FloodingIndex,
        decimal DrainageSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal PumpCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal BlockageIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        string? BudgetAuthorizationStatus,
        string? BudgetAuthorizationLevel,
        decimal? BudgetAvailableAmount,
        bool? BudgetAuthorizedByEmergencyOverride,
        string? BudgetAuthorizedIntensity,
        string? BudgetAuthorizationSummary,
        CityDrainageSystemStatusDto System)
    {
        public static CityDrainageStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal drainageSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null,
            string? budgetAuthorizationStatus = null,
            string? budgetAuthorizationLevel = null,
            decimal? budgetAvailableAmount = null,
            bool? budgetAuthorizedByEmergencyOverride = null,
            string? budgetAuthorizedIntensity = null,
            string? budgetAuthorizationSummary = null)
        {
            return new CityDrainageStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                FloodingIndex: state.FloodingIndex.Value,
                DrainageSupportIndex: drainageSupportIndex,
                BudgetPressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EmergencyModeEnabled: state.DrainageInfrastructure.EmergencyModeEnabled,
                PumpCapacityIndex: state.DrainageInfrastructure.PumpCapacityIndex,
                NetworkIntegrityIndex: state.DrainageInfrastructure.NetworkIntegrityIndex,
                BlockageIndex: state.DrainageInfrastructure.BlockageIndex,
                CrewReadinessIndex: state.DrainageInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.DrainageInfrastructure.IncidentPressureIndex,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: appliedIntensity,
                BudgetAuthorizationStatus: budgetAuthorizationStatus,
                BudgetAuthorizationLevel: budgetAuthorizationLevel,
                BudgetAvailableAmount: budgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: budgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: budgetAuthorizedIntensity,
                BudgetAuthorizationSummary: budgetAuthorizationSummary,
                System: CityDrainageSystemStatusDto.FromSnapshot(state.Drainage.ToSnapshot()));
        }
    }
}
