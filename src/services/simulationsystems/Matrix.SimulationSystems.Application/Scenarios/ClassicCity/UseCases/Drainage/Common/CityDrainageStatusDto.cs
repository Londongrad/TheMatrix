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
        CityDrainageSystemStatusDto System)
    {
        public static CityDrainageStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal drainageSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null)
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
                System: CityDrainageSystemStatusDto.FromSnapshot(state.Drainage.ToSnapshot()));
        }
    }
}
