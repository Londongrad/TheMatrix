using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.Abstractions;

namespace Matrix.Economy.Infrastructure.Messaging
{
    public sealed class MassTransitCityPopulationSignalPublisher(IPublishEndpoint publishEndpoint)
        : ICityPopulationSignalPublisher
    {
        public Task PublishClassicCityEmployerFinancialStressBatchAsync(
            ClassicCityEmployerFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            return publishEndpoint.Publish(
                message: batch,
                cancellationToken: cancellationToken);
        }

        public Task PublishClassicCityHouseholdFinancialStressBatchAsync(
            ClassicCityHouseholdFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            return publishEndpoint.Publish(
                message: batch,
                cancellationToken: cancellationToken);
        }
    }
}
