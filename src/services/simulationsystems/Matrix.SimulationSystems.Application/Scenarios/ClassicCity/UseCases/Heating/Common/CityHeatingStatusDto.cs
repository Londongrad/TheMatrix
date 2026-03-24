using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common
{
    public sealed record CityHeatingStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal HeatingCoverageIndex,
        decimal HeatingSupportIndex,
        bool EmergencyModeEnabled,
        decimal PlantCapacityIndex,
        decimal NetworkIntegrityIndex,
        decimal ControlReadinessIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CityHeatingSystemStatusDto System)
    {
        public static CityHeatingStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal heatingSupportIndex)
        {
            return new CityHeatingStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                HeatingCoverageIndex: state.HeatingCoverageIndex.Value,
                HeatingSupportIndex: heatingSupportIndex,
                EmergencyModeEnabled: state.HeatingInfrastructure.EmergencyModeEnabled,
                PlantCapacityIndex: state.HeatingInfrastructure.PlantCapacityIndex,
                NetworkIntegrityIndex: state.HeatingInfrastructure.NetworkIntegrityIndex,
                ControlReadinessIndex: state.HeatingInfrastructure.ControlReadinessIndex,
                CrewReadinessIndex: state.HeatingInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.HeatingInfrastructure.IncidentPressureIndex,
                System: CityHeatingSystemStatusDto.FromSnapshot(state.Heating.ToSnapshot()));
        }
    }
}
