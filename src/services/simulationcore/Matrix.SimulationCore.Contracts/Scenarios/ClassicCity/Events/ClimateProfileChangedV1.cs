using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events
{
    public sealed record ClimateProfileChangedV1(
        Guid CityId,
        WeatherClimateProfileV1 PreviousProfile,
        WeatherClimateProfileV1 CurrentProfile,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
