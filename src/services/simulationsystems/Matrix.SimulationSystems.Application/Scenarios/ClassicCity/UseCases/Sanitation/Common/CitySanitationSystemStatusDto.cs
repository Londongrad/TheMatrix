using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common
{
    public sealed record CitySanitationSystemStatusDto(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex)
    {
        public static CitySanitationSystemStatusDto FromSnapshot(CitySystemSnapshot snapshot)
        {
            return new CitySanitationSystemStatusDto(
                Kind: snapshot.Kind.ToString(),
                LoadIndex: snapshot.LoadIndex,
                ServiceQualityIndex: snapshot.ServiceQualityIndex,
                BacklogIndex: snapshot.BacklogIndex,
                FailureRiskIndex: snapshot.FailureRiskIndex);
        }
    }
}
