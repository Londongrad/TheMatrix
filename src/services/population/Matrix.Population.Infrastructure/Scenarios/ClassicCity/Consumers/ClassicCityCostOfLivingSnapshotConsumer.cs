using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityCostOfLivingSnapshot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityCostOfLivingSnapshotConsumer(
        IMediator mediator,
        ILogger<ClassicCityCostOfLivingSnapshotConsumer> logger)
        : IConsumer<ClassicCityCostOfLivingSnapshotV1>
    {
        public Task Consume(ConsumeContext<ClassicCityCostOfLivingSnapshotV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityCostOfLivingSnapshotV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException(
                    "ClassicCityCostOfLivingSnapshot message must have a MessageId.");

            ApplyCityCostOfLivingSnapshotResult result = await mediator.Send(
                request: new ApplyCityCostOfLivingSnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: ClassicCityCostOfLivingSnapshotConsumerDefinition.EndpointNameValue,
                    WageMultiplier: message.WageMultiplier,
                    RetailPriceMultiplier: message.RetailPriceMultiplier,
                    HousingCostMultiplier: message.HousingCostMultiplier,
                    UtilityCostMultiplier: message.UtilityCostMultiplier,
                    CostOfLivingIndex: message.CostOfLivingIndex,
                    AffordabilityIndex: message.AffordabilityIndex,
                    OccurredAtUtc: message.OccurredAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityCostOfLivingSnapshotStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city cost-of-living snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city cost-of-living snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city cost-of-living snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city cost-of-living snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.Stale:
                    logger.LogDebug(
                        message:
                        "Skipped stale classic city cost-of-living snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
