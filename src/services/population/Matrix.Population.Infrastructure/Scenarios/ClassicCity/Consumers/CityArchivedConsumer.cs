using MassTransit;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityArchivedConsumer(
        IMediator mediator,
        ILogger<CityArchivedConsumer> logger) : IConsumer<SimulationArchivedV1>
    {
        public Task Consume(ConsumeContext<SimulationArchivedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationArchivedV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey))
                return;

            if (messageId is null)
                throw new InvalidOperationException("SimulationArchived message must have a MessageId.");

            ArchiveCityPopulationDataResult result = await mediator.Send(
                request: new ArchiveCityPopulationDataCommand(
                    CityId: message.HostId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: CityArchivedConsumerDefinition.EndpointNameValue,
                    ArchivedAtUtc: message.ArchivedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ArchiveCityPopulationDataStatus.Applied:
                    logger.LogInformation(
                        message: "Archived population activity for cityId={CityId}.",
                        message.HostId);
                    break;

                case ArchiveCityPopulationDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city archive handling for cityId={CityId}.",
                        message.HostId);
                    break;

                case ArchiveCityPopulationDataStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale city archive handling for cityId={CityId}.",
                        message.HostId);
                    break;

                case ArchiveCityPopulationDataStatus.CityDeleted:
                    logger.LogDebug(
                        message: "Skipped city archive handling for deleted cityId={CityId}.",
                        message.HostId);
                    break;
            }
        }
    }
}
