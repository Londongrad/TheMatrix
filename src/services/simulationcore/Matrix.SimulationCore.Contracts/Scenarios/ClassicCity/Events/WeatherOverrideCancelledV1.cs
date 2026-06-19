using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events
{
    public sealed record WeatherOverrideCancelledV1(
        Guid CityId,
        WeatherStateV1 ForcedState,
        string Source,
        DateTimeOffset CancelledAtUtc,
        DateTime OccurredOnUtc);
}
