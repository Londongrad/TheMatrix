using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common
{
    public sealed record CitySanitationStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SanitationCoverageIndex,
        decimal SanitationSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal TreatmentStabilityIndex,
        decimal NetworkIntegrityIndex,
        decimal OverflowControlIndex,
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
        CitySanitationSystemStatusDto System)
    {
        public static CitySanitationStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal sanitationSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null,
            string? budgetAuthorizationStatus = null,
            string? budgetAuthorizationLevel = null,
            decimal? budgetAvailableAmount = null,
            bool? budgetAuthorizedByEmergencyOverride = null,
            string? budgetAuthorizedIntensity = null,
            string? budgetAuthorizationSummary = null)
        {
            return new CitySanitationStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                SanitationCoverageIndex: state.SanitationCoverageIndex.Value,
                SanitationSupportIndex: sanitationSupportIndex,
                BudgetPressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EmergencyModeEnabled: state.SanitationInfrastructure.EmergencyModeEnabled,
                TreatmentStabilityIndex: state.SanitationInfrastructure.TreatmentStabilityIndex,
                NetworkIntegrityIndex: state.SanitationInfrastructure.NetworkIntegrityIndex,
                OverflowControlIndex: state.SanitationInfrastructure.OverflowControlIndex,
                CrewReadinessIndex: state.SanitationInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.SanitationInfrastructure.IncidentPressureIndex,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: appliedIntensity,
                BudgetAuthorizationStatus: budgetAuthorizationStatus,
                BudgetAuthorizationLevel: budgetAuthorizationLevel,
                BudgetAvailableAmount: budgetAvailableAmount,
                BudgetAuthorizedByEmergencyOverride: budgetAuthorizedByEmergencyOverride,
                BudgetAuthorizedIntensity: budgetAuthorizedIntensity,
                BudgetAuthorizationSummary: budgetAuthorizationSummary,
                PendingOperation: PendingCityOperationDto.FromDomain(state.PendingSanitationMaintenance),
                System: CitySanitationSystemStatusDto.FromSnapshot(state.Sanitation.ToSnapshot()));
        }
    }
}
