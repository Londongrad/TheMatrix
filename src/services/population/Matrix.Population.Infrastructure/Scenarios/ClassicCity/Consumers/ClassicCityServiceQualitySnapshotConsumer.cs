using MassTransit;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityServiceQualitySnapshot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityServiceQualitySnapshotConsumer(
        IMediator mediator,
        ILogger<ClassicCityServiceQualitySnapshotConsumer> logger)
        : IConsumer<ClassicCityServiceQualitySnapshotV1>
    {
        public Task Consume(ConsumeContext<ClassicCityServiceQualitySnapshotV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityServiceQualitySnapshotV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException("ClassicCityServiceQualitySnapshot message must have a MessageId.");

            ApplyCityServiceQualitySnapshotResult result = await mediator.Send(
                request: new ApplyCityServiceQualitySnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: ClassicCityServiceQualitySnapshotConsumerDefinition.EndpointNameValue,
                    HealthcareQualityIndex: message.HealthcareQualityIndex,
                    HousingSupportIndex: message.HousingSupportIndex,
                    OccurredAtUtc: message.OccurredAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityServiceQualitySnapshotStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city service-quality snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city service-quality snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city service-quality snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city service-quality snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityServiceQualitySnapshotStatus.Stale:
                    logger.LogDebug(
                        message:
                        "Skipped stale classic city service-quality snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
