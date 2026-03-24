using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common
{
    public sealed record CitySnowRemovalSystemStatusDto(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex)
    {
        public static CitySnowRemovalSystemStatusDto FromSnapshot(CitySystemSnapshot snapshot)
        {
            return new CitySnowRemovalSystemStatusDto(
                Kind: snapshot.Kind.ToString(),
                LoadIndex: snapshot.LoadIndex,
                ServiceQualityIndex: snapshot.ServiceQualityIndex,
                BacklogIndex: snapshot.BacklogIndex,
                FailureRiskIndex: snapshot.FailureRiskIndex);
        }
    }
}
