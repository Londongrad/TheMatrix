using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common
{
    public sealed record CityDrainageStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal FloodingIndex,
        decimal DrainageSupportIndex,
        bool EmergencyModeEnabled,
        decimal PumpCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal BlockageIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityDrainageSystemStatusDto System)
    {
        public static CityDrainageStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal drainageSupportIndex)
        {
            return new CityDrainageStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                FloodingIndex: state.FloodingIndex.Value,
                DrainageSupportIndex: drainageSupportIndex,
                EmergencyModeEnabled: state.DrainageInfrastructure.EmergencyModeEnabled,
                PumpCapacityIndex: state.DrainageInfrastructure.PumpCapacityIndex,
                NetworkIntegrityIndex: state.DrainageInfrastructure.NetworkIntegrityIndex,
                BlockageIndex: state.DrainageInfrastructure.BlockageIndex,
                CrewReadinessIndex: state.DrainageInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.DrainageInfrastructure.IncidentPressureIndex,
                System: CityDrainageSystemStatusDto.FromSnapshot(state.Drainage.ToSnapshot()));
        }
    }
}
