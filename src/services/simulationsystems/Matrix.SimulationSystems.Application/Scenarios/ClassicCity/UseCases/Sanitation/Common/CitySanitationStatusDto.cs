using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common
{
    public sealed record CitySanitationStatusDto(
        Guid CityId,
        DateTimeOffset LastEvaluatedAtUtc,
        decimal SanitationCoverageIndex,
        decimal SanitationSupportIndex,
        bool EmergencyModeEnabled,
        decimal TreatmentStabilityIndex,
        decimal NetworkIntegrityIndex,
        decimal OverflowControlIndex,
        decimal CrewReadinessIndex,
        decimal IncidentPressureIndex,
        CitySanitationSystemStatusDto System)
    {
        public static CitySanitationStatusDto FromState(
            Guid cityId,
            CityEnvironmentalConditionState state,
            decimal sanitationSupportIndex)
        {
            return new CitySanitationStatusDto(
                CityId: cityId,
                LastEvaluatedAtUtc: state.LastEvaluatedAtUtc,
                SanitationCoverageIndex: state.SanitationCoverageIndex.Value,
                SanitationSupportIndex: sanitationSupportIndex,
                EmergencyModeEnabled: state.SanitationInfrastructure.EmergencyModeEnabled,
                TreatmentStabilityIndex: state.SanitationInfrastructure.TreatmentStabilityIndex,
                NetworkIntegrityIndex: state.SanitationInfrastructure.NetworkIntegrityIndex,
                OverflowControlIndex: state.SanitationInfrastructure.OverflowControlIndex,
                CrewReadinessIndex: state.SanitationInfrastructure.CrewReadinessIndex,
                IncidentPressureIndex: state.SanitationInfrastructure.IncidentPressureIndex,
                System: CitySanitationSystemStatusDto.FromSnapshot(state.Sanitation.ToSnapshot()));
        }
    }
}
