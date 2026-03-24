using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common
{
    public sealed record CityRoadAccessSystemStatusDto(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex)
    {
        public static CityRoadAccessSystemStatusDto FromSnapshot(CitySystemSnapshot snapshot)
        {
            return new CityRoadAccessSystemStatusDto(
                Kind: snapshot.Kind.ToString(),
                LoadIndex: snapshot.LoadIndex,
                ServiceQualityIndex: snapshot.ServiceQualityIndex,
                BacklogIndex: snapshot.BacklogIndex,
                FailureRiskIndex: snapshot.FailureRiskIndex);
        }
    }
}
