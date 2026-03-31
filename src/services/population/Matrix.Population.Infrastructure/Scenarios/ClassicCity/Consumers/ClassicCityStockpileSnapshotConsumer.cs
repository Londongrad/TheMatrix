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
        public async Task Consume(ConsumeContext<ClassicCityStockpileSnapshotV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException(
                    "ClassicCityStockpileSnapshot message must have a MessageId.");

            ClassicCityStockpileSnapshotV1 message = context.Message;

            ApplyCityEssentialsSnapshotResult result = await mediator.Send(
                new ApplyCityEssentialsSnapshotCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
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
                context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityEssentialsSnapshotStatus.Applied:
                    logger.LogInformation(
                        "Applied classic city essentials snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.Duplicate:
                    logger.LogDebug(
                        "Skipped duplicate classic city essentials snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.CityDeleted:
                    logger.LogDebug(
                        "Skipped classic city essentials snapshot for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.CityArchived:
                    logger.LogDebug(
                        "Skipped classic city essentials snapshot for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
                case ApplyCityEssentialsSnapshotStatus.Stale:
                    logger.LogDebug(
                        "Skipped stale classic city essentials snapshot for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
