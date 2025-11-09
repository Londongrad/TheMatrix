namespace Matrix.CityCore.Application.IntegrationEvents
{
    /// <summary>
    /// Интеграционное событие: игровой месяц завершился.
    /// Его будут слушать Population, Economy и др.
    /// </summary>
    public sealed record SimulationMonthEndedIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        int Year,
        int Month);
}
