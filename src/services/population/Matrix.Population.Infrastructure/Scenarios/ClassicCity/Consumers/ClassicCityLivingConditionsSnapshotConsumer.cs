using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityLivingConditionsSnapshot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityLivingConditionsSnapshotConsumer(
        IMediator mediator,
        ILogger<ClassicCityLivingConditionsSnapshotConsumer> logger)
        : IConsumer<ClassicCityLivingConditionsSnapshotV1>
    {
        public Task Consume(ConsumeContext<ClassicCityLivingConditionsSnapshotV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityLivingConditionsSnapshotV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException(
                    "ClassicCityLivingConditionsSnapshot message must have a MessageId.");

            ApplyCityLivingConditionsSnapshotResult result = await mediator.Send(
                request: new ApplyCityLivingConditionsSnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: ClassicCityLivingConditionsSnapshotConsumerDefinition.EndpointNameValue,
                    FloodingIndex: message.FloodingIndex,
                    RoadAccessibilityIndex: message.RoadAccessibilityIndex,
                    PowerCoverageIndex: message.PowerCoverageIndex,
                    UtilityContinuityIndex: message.UtilityContinuityIndex,
                    HeatingCoverageIndex: message.HeatingCoverageIndex,
                    WaterCoverageIndex: message.WaterCoverageIndex,
                    SanitationCoverageIndex: message.SanitationCoverageIndex,
                    EffectiveTickId: message.EffectiveTickId,
                    EffectiveAtUtc: message.EffectiveAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityLivingConditionsSnapshotStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city living-conditions snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city living-conditions snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city living-conditions snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city living-conditions snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.Stale:
                    logger.LogDebug(
                        message:
                        "Skipped stale classic city living-conditions snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
