namespace Matrix.CityCore.Contracts.Events
{
    public sealed record WeatherOverrideCancelledV1(
        Guid CityId,
        WeatherStateV1 ForcedState,
        string Source,
        DateTimeOffset CancelledAtUtc,
        DateTime OccurredOnUtc);
}
