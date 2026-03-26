using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common
{
    public sealed record CityPowerDistributionStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal PowerCoverageIndex,
        decimal PowerSupportIndex,
        bool EmergencyModeEnabled,
        decimal SubstationCapacityIndex,
        decimal GridIntegrityIndex,
        decimal SwitchingReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityPowerDistributionSystemStatusDto System)
    {
        public static CityPowerDistributionStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal powerSupportIndex)
        {
            return new CityPowerDistributionStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                PowerCoverageIndex: state.PowerCoverageIndex.Value,
                PowerSupportIndex: powerSupportIndex,
                EmergencyModeEnabled: state.PowerDistributionInfrastructure.EmergencyModeEnabled,
                SubstationCapacityIndex: state.PowerDistributionInfrastructure.SubstationCapacityIndex,
                GridIntegrityIndex: state.PowerDistributionInfrastructure.GridIntegrityIndex,
                SwitchingReadinessIndex: state.PowerDistributionInfrastructure.SwitchingReadinessIndex,
                CrewReadinessIndex: state.PowerDistributionInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.PowerDistributionInfrastructure.IncidentPressureIndex,
                System: CityPowerDistributionSystemStatusDto.FromSnapshot(state.PowerDistribution.ToSnapshot()));
        }
    }
}
