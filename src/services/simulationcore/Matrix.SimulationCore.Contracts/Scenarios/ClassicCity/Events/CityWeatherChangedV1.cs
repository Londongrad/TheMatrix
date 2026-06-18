using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events
{
    public sealed record CityWeatherChangedV1(
        Guid CityId,
        WeatherStateV1 PreviousState,
        WeatherStateV1 CurrentState,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc);
}
