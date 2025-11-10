using Matrix.Population.Application.IntegrationEvents;

namespace Matrix.Population.Application.Abstractions
{
    public interface IMonthlyIncomeEventPublisher
    {
        Task PublishAsync(
            MonthlyIncomeEarnedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default);
    }
}
