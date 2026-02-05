using MassTransit;
using MediatR;
using Matrix.CityCore.Contracts.Events;
using Matrix.Population.Application.UseCases.Population.DeleteCityPopulationData;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Consumers
{
    public sealed class CityDeletedConsumer(
        IMediator mediator,
        ILogger<CityDeletedConsumer> logger) : IConsumer<CityDeletedV1>
    {
        public async Task Consume(ConsumeContext<CityDeletedV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException("CityDeleted message must have a MessageId.");

            CityDeletedV1 message = context.Message;

            DeleteCityPopulationDataResult result = await mediator.Send(
                request: new DeleteCityPopulationDataCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
                    ConsumerName: CityDeletedConsumerDefinition.EndpointNameValue,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case DeleteCityPopulationDataStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted population data for cityId={CityId}.",
                        message.CityId);
                    break;

                case DeleteCityPopulationDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate city deletion cleanup for cityId={CityId}.",
                        message.CityId);
                    break;

                case DeleteCityPopulationDataStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale city deletion cleanup for cityId={CityId}.",
                        message.CityId);
                    break;
            }
        }
    }
}
