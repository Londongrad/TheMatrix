using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
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
        public async Task Consume(ConsumeContext<ClassicCityLivingConditionsSnapshotV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException(
                    "ClassicCityLivingConditionsSnapshot message must have a MessageId.");

            ClassicCityLivingConditionsSnapshotV1 message = context.Message;

            ApplyCityLivingConditionsSnapshotResult result = await mediator.Send(
                new ApplyCityLivingConditionsSnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
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
                context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityLivingConditionsSnapshotStatus.Applied:
                    logger.LogInformation(
                        "Applied classic city living-conditions snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.Duplicate:
                    logger.LogDebug(
                        "Skipped duplicate classic city living-conditions snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        "Skipped classic city living-conditions snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.CityArchived:
                    logger.LogDebug(
                        "Skipped classic city living-conditions snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityLivingConditionsSnapshotStatus.Stale:
                    logger.LogDebug(
                        "Skipped stale classic city living-conditions snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
