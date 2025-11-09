using Matrix.CityCore.Application.IntegrationEvents;

namespace Matrix.CityCore.Application.Abstractions
{
    public interface ICityIntegrationEventPublisher
    {
        Task PublishSimulationMonthEndedAsync(
            SimulationMonthEndedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default);

        // На будущее – можно добавить другие методы, пока хватит месяца
    }
}
