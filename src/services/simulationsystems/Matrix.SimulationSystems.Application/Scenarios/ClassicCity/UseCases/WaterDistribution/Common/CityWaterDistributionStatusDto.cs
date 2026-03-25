using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common
{
    public sealed record CityWaterDistributionStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal WaterCoverageIndex,
        decimal WaterSupportIndex,
        bool EmergencyModeEnabled,
        decimal TreatmentCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal PumpReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityWaterDistributionSystemStatusDto System)
    {
        public static CityWaterDistributionStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal waterSupportIndex)
        {
            return new CityWaterDistributionStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                WaterCoverageIndex: state.WaterCoverageIndex.Value,
                WaterSupportIndex: waterSupportIndex,
                EmergencyModeEnabled: state.WaterDistributionInfrastructure.EmergencyModeEnabled,
                TreatmentCapacityIndex: state.WaterDistributionInfrastructure.TreatmentCapacityIndex,
                NetworkIntegrityIndex: state.WaterDistributionInfrastructure.NetworkIntegrityIndex,
                PumpReadinessIndex: state.WaterDistributionInfrastructure.PumpReadinessIndex,
                CrewReadinessIndex: state.WaterDistributionInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.WaterDistributionInfrastructure.IncidentPressureIndex,
                System: CityWaterDistributionSystemStatusDto.FromSnapshot(state.WaterDistribution.ToSnapshot()));
        }
    }
}
