using Matrix.CityCore.Application.Abstractions;
using Matrix.CityCore.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Matrix.CityCore.Infrastructure.Messaging
{
    public sealed class LoggingCityIntegrationEventPublisher(ILogger<LoggingCityIntegrationEventPublisher> logger)
        : ICityIntegrationEventPublisher
    {
        private readonly ILogger<LoggingCityIntegrationEventPublisher> _logger = logger;

        public Task PublishSimulationMonthEndedAsync(
            SimulationMonthEndedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("SimulationMonthEnded published {@Event}", integrationEvent);
            return Task.CompletedTask;
        }
    }
}
