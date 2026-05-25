using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEssentialsSnapshot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityStockpileSnapshotConsumer(
        IMediator mediator,
        ILogger<ClassicCityStockpileSnapshotConsumer> logger)
        : IConsumer<ClassicCityStockpileSnapshotV1>
    {
        public Task Consume(ConsumeContext<ClassicCityStockpileSnapshotV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityStockpileSnapshotV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException("ClassicCityStockpileSnapshot message must have a MessageId.");

            ApplyCityEssentialsSnapshotResult result = await mediator.Send(
                request: new ApplyCityEssentialsSnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: ClassicCityStockpileSnapshotConsumerDefinition.EndpointNameValue,
                    SupplyStressIndex: message.SupplyStressIndex,
                    EmergencyRationingEnabled: message.EmergencyRationingEnabled,
                    FoodStockLevelIndex: message.Food.StockLevelIndex,
                    FoodShortageRiskIndex: message.Food.ShortageRiskIndex,
                    MedicineStockLevelIndex: message.Medicine.StockLevelIndex,
                    MedicineShortageRiskIndex: message.Medicine.ShortageRiskIndex,
                    EmergencyWaterStockLevelIndex: message.EmergencyWater.StockLevelIndex,
                    EmergencyWaterShortageRiskIndex: message.EmergencyWater.ShortageRiskIndex,
                    EffectiveTickId: message.EffectiveTickId,
                    EffectiveAtUtc: message.EffectiveAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityEssentialsSnapshotStatus.Applied:
                    logger.LogInformation(
                        message: "Applied classic city essentials snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city essentials snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city essentials snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city essentials snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.Stale:
                    logger.LogDebug(
                        message:
                        "Skipped stale classic city essentials snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
