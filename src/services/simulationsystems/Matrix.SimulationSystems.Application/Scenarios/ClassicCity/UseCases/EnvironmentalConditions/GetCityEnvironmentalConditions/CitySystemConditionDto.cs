using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    GetCityEnvironmentalConditions
{
    public sealed record CitySystemConditionDto(
        string Kind,
        decimal LoadIndex,
        decimal ServiceQualityIndex,
        decimal BacklogIndex,
        decimal FailureRiskIndex)
    {
        public static CitySystemConditionDto FromSnapshot(CitySystemSnapshot snapshot)
        {
            return new CitySystemConditionDto(
                Kind: snapshot.Kind.ToString(),
                LoadIndex: snapshot.LoadIndex,
                ServiceQualityIndex: snapshot.ServiceQualityIndex,
                BacklogIndex: snapshot.BacklogIndex,
                FailureRiskIndex: snapshot.FailureRiskIndex);
        }
    }
}
