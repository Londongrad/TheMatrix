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
        public async Task Consume(ConsumeContext<ClassicCityCostOfLivingSnapshotV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException(
                    "ClassicCityCostOfLivingSnapshot message must have a MessageId.");

            ClassicCityCostOfLivingSnapshotV1 message = context.Message;

            ApplyCityCostOfLivingSnapshotResult result = await mediator.Send(
                request: new ApplyCityCostOfLivingSnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
                    ConsumerName: ClassicCityCostOfLivingSnapshotConsumerDefinition.EndpointNameValue,
                    WageMultiplier: message.WageMultiplier,
                    RetailPriceMultiplier: message.RetailPriceMultiplier,
                    HousingCostMultiplier: message.HousingCostMultiplier,
                    UtilityCostMultiplier: message.UtilityCostMultiplier,
                    CostOfLivingIndex: message.CostOfLivingIndex,
                    AffordabilityIndex: message.AffordabilityIndex,
                    OccurredAtUtc: message.OccurredAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityCostOfLivingSnapshotStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city cost-of-living snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city cost-of-living snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city cost-of-living snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city cost-of-living snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityCostOfLivingSnapshotStatus.Stale:
                    logger.LogDebug(
                        message:
                        "Skipped stale classic city cost-of-living snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
