namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record WeatherOverrideCancelledIntegrationEvent(
        Guid CityId,
        WeatherStateIntegrationData ForcedState,
        string Source,
        DateTimeOffset CancelledAtUtc,
        DateTime OccurredOnUtc);
}