using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityArchivedConsumer(
        IMediator mediator,
        ILogger<CityArchivedConsumer> logger) : IConsumer<CityArchivedV1>
    {
        public Task Consume(ConsumeContext<CityArchivedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityArchivedV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException("CityArchived message must have a MessageId.");

            ArchiveCityPopulationDataResult result = await mediator.Send(
                request: new ArchiveCityPopulationDataCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: CityArchivedConsumerDefinition.EndpointNameValue,
                    ArchivedAtUtc: message.ArchivedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ArchiveCityPopulationDataStatus.Applied:
                    logger.LogInformation(
                        message: "Archived population activity for cityId={CityId}.",
                        message.CityId);
                    break;

                case ArchiveCityPopulationDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city archive handling for cityId={CityId}.",
                        message.CityId);
                    break;

                case ArchiveCityPopulationDataStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale city archive handling for cityId={CityId}.",
                        message.CityId);
                    break;

                case ArchiveCityPopulationDataStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city archive handling for deleted cityId={CityId}.",
                        message.CityId);
                    break;
            }
        }
    }
}
