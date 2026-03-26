using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common
{
    public sealed record CityUtilityIncidentSystemStatusDto(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex)
    {
        public static CityUtilityIncidentSystemStatusDto FromSnapshot(CitySystemSnapshot snapshot)
        {
            return new CityUtilityIncidentSystemStatusDto(
                Kind: snapshot.Kind.ToString(),
                LoadIndex: snapshot.LoadIndex,
                ServiceQualityIndex: snapshot.ServiceQualityIndex,
                BacklogIndex: snapshot.BacklogIndex,
                FailureRiskIndex: snapshot.FailureRiskIndex);
        }
    }
}
