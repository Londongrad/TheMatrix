using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events
{
    public sealed record WeatherOverrideExpiredV1(
        Guid CityId,
        WeatherStateV1 ForcedState,
        string Source,
        DateTimeOffset ExpiredAtUtc,
        DateTime OccurredOnUtc);
}
