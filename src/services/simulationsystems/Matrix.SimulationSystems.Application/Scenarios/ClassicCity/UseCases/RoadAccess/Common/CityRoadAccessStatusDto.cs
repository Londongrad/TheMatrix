using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
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
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal CorridorAvailabilityIndex,
        decimal SurfaceIntegrityIndex,
        decimal TrafficControlReadinessIndex,
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
        CityRoadAccessSystemStatusDto System)
    {
        public static CityRoadAccessStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal roadSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null,
            string? budgetAuthorizationStatus = null,
            string? budgetAuthorizationLevel = null,
            decimal? budgetAvailableAmount = null,
            bool? budgetAuthorizedByEmergencyOverride = null,
            string? budgetAuthorizedIntensity = null,
            string? budgetAuthorizationSummary = null)
        {
            return new CityRoadAccessStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                FloodingIndex: state.FloodingIndex.Value,
                SnowAccumulationIndex: state.SnowAccumulationIndex.Value,
                RoadAccessibilityIndex: state.RoadAccessibilityIndex.Value,
                RoadSupportIndex: roadSupportIndex,
                BudgetPressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EmergencyModeEnabled: state.RoadAccessInfrastructure.EmergencyModeEnabled,
                CorridorAvailabilityIndex: state.RoadAccessInfrastructure.CorridorAvailabilityIndex,
                SurfaceIntegrityIndex: state.RoadAccessInfrastructure.SurfaceIntegrityIndex,
                TrafficControlReadinessIndex: state.RoadAccessInfrastructure.TrafficControlReadinessIndex,
                CrewReadinessIndex: state.RoadAccessInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.RoadAccessInfrastructure.IncidentPressureIndex,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: appliedIntensity,
                BudgetAuthorizationStatus: budgetAuthorizationStatus,
                BudgetAuthorizationLevel: budgetAuthorizationLevel,
                BudgetAvailableAmount: budgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: budgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: budgetAuthorizedIntensity,
                BudgetAuthorizationSummary: budgetAuthorizationSummary,
                PendingOperation: PendingCityOperationDto.FromDomain(state.PendingRoadAccessMaintenance),
                System: CityRoadAccessSystemStatusDto.FromSnapshot(state.RoadAccess.ToSnapshot()));
        }
    }
}
