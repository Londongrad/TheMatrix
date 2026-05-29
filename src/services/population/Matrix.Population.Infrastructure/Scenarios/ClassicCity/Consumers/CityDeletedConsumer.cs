using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumer(
        IMediator mediator,
        ILogger<CityDeletedConsumer> logger) : IConsumer<SimulationDeletedV1>
    {
        public Task Consume(ConsumeContext<SimulationDeletedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationDeletedV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey))
                return;

            if (messageId is null)
                throw new InvalidOperationException("SimulationDeleted message must have a MessageId.");

            DeleteCityPopulationDataResult result = await mediator.Send(
                request: new DeleteCityPopulationDataCommand(
                    CityId: message.HostId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: CityDeletedConsumerDefinition.EndpointNameValue,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteCityPopulationDataStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted population data for cityId={CityId}.",
                        message.HostId);
                    break;

                case DeleteCityPopulationDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city deletion cleanup for cityId={CityId}.",
                        message.HostId);
                    break;

                case DeleteCityPopulationDataStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale city deletion cleanup for cityId={CityId}.",
                        message.HostId);
                    break;
            }
        }
    }
}
