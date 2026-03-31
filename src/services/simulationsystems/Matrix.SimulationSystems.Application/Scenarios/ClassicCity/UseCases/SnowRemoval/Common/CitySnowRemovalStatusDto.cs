using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common
{
    public sealed record CitySnowRemovalStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SnowAccumulationIndex,
        decimal RoadAccessibilityIndex,
        decimal SnowRemovalSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal FleetAvailabilityIndex,
        decimal RouteCoverageIndex,
        decimal DeicingReadinessIndex,
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
        PendingCityOperationDto? PendingOperation,
        CitySnowRemovalSystemStatusDto System)
    {
        public static CitySnowRemovalStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal snowRemovalSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null,
            string? budgetAuthorizationStatus = null,
            string? budgetAuthorizationLevel = null,
            decimal? budgetAvailableAmount = null,
            bool? budgetAuthorizedByEmergencyOverride = null,
            string? budgetAuthorizedIntensity = null,
            string? budgetAuthorizationSummary = null)
        {
            return new CitySnowRemovalStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                SnowRemovalSupportIndex: snowRemovalSupportIndex,
                BudgetPressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EmergencyModeEnabled: state.SnowRemovalInfrastructure.EmergencyModeEnabled,
                FleetAvailabilityIndex: state.SnowRemovalInfrastructure.FleetAvailabilityIndex,
                RouteCoverageIndex: state.SnowRemovalInfrastructure.RouteCoverageIndex,
                DeicingReadinessIndex: state.SnowRemovalInfrastructure.DeicingReadinessIndex,
                CrewReadinessIndex: state.SnowRemovalInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.SnowRemovalInfrastructure.IncidentPressureIndex,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: appliedIntensity,
                BudgetAuthorizationStatus: budgetAuthorizationStatus,
                BudgetAuthorizationLevel: budgetAuthorizationLevel,
                BudgetAvailableAmount: budgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: budgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: budgetAuthorizedIntensity,
                BudgetAuthorizationSummary: budgetAuthorizationSummary,
                PendingOperation: PendingCityOperationDto.FromDomain(state.PendingSnowRemovalMaintenance),
                System: CitySnowRemovalSystemStatusDto.FromSnapshot(state.SnowRemoval.ToSnapshot()));
        }
    }
}
