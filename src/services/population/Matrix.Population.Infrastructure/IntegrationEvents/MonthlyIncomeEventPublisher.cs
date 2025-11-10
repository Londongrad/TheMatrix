using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.IntegrationEvents
{
    internal sealed class MonthlyIncomeEventPublisher(ILogger<MonthlyIncomeEventPublisher> logger)
        : IMonthlyIncomeEventPublisher
    {
        private readonly ILogger<MonthlyIncomeEventPublisher> _logger = logger;

        public Task PublishAsync(
            MonthlyIncomeEarnedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            // TODO: здесь позже будет отправка в брокер сообщений / Economica / ещё куда-то
            _logger.LogInformation(
                "Publishing MonthlyIncomeEarnedIntegrationEvent: {@Event}",
                integrationEvent);

            // Пока просто считаем, что успешно
            return Task.CompletedTask;
        }
    }
}
