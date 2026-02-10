using MassTransit;
using Matrix.CityCore.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityArchivedConsumer(
        IMediator mediator,
        ILogger<CityArchivedConsumer> logger) : IConsumer<CityArchivedV1>
    {
        public async Task Consume(ConsumeContext<CityArchivedV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException("CityArchived message must have a MessageId.");

            CityArchivedV1 message = context.Message;

            ArchiveCityPopulationDataResult result = await mediator.Send(
                request: new ArchiveCityPopulationDataCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
                    ConsumerName: CityArchivedConsumerDefinition.EndpointNameValue,
                    ArchivedAtUtc: message.ArchivedAtUtc),
                cancellationToken: context.CancellationToken);

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
