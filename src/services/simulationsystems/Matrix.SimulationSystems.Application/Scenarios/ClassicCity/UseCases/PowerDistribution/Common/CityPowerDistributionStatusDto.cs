using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common
{
    public sealed record CityPowerDistributionStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal PowerCoverageIndex,
        decimal PowerSupportIndex,
        decimal BudgetPressureIndex,
        bool EmergencyModeEnabled,
        decimal SubstationCapacityIndex,
        decimal GridIntegrityIndex,
        decimal SwitchingReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        string? RequestedIntensity,
        string? AppliedIntensity,
        CityPowerDistributionSystemStatusDto System)
    {
        public static CityPowerDistributionStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal powerSupportIndex,
            string? requestedIntensity = null,
            string? appliedIntensity = null)
        {
            return new CityPowerDistributionStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                PowerCoverageIndex: state.PowerCoverageIndex.Value,
                PowerSupportIndex: powerSupportIndex,
                BudgetPressureIndex: state.OperationalBudgetPressure.PressureIndex,
                EmergencyModeEnabled: state.PowerDistributionInfrastructure.EmergencyModeEnabled,
                SubstationCapacityIndex: state.PowerDistributionInfrastructure.SubstationCapacityIndex,
                GridIntegrityIndex: state.PowerDistributionInfrastructure.GridIntegrityIndex,
                SwitchingReadinessIndex: state.PowerDistributionInfrastructure.SwitchingReadinessIndex,
                CrewReadinessIndex: state.PowerDistributionInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.PowerDistributionInfrastructure.IncidentPressureIndex,
                RequestedIntensity: requestedIntensity,
                AppliedIntensity: appliedIntensity,
                System: CityPowerDistributionSystemStatusDto.FromSnapshot(state.PowerDistribution.ToSnapshot()));
        }
    }
}
