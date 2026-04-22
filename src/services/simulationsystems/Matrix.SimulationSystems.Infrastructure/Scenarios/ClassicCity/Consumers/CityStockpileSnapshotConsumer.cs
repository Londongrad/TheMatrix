using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityStockpileSnapshotConsumer(
        IMediator mediator,
        ILogger<CityStockpileSnapshotConsumer> logger) : IConsumer<ClassicCityStockpileSnapshotV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityStockpileSnapshotV1> context)
        {
            ClassicCityStockpileSnapshotV1 message = context.Message;

            SyncCityResourceSupplyResult result = await mediator.Send(
                request: new SyncCityResourceSupplyCommand(
                    CityId: message.CityId,
                    SupplyStressIndex: message.SupplyStressIndex,
                    FuelStockLevelIndex: message.Fuel.StockLevelIndex,
                    FuelResupplyReadinessIndex: message.Fuel.ResupplyReadinessIndex,
                    FuelShortageRiskIndex: message.Fuel.ShortageRiskIndex,
                    SparePartsStockLevelIndex: message.SpareParts.StockLevelIndex,
                    SparePartsResupplyReadinessIndex: message.SpareParts.ResupplyReadinessIndex,
                    SparePartsShortageRiskIndex: message.SpareParts.ShortageRiskIndex,
                    FiltersStockLevelIndex: message.Filters.StockLevelIndex,
                    FiltersResupplyReadinessIndex: message.Filters.ResupplyReadinessIndex,
                    FiltersShortageRiskIndex: message.Filters.ShortageRiskIndex,
                    EmergencyWaterStockLevelIndex: message.EmergencyWater.StockLevelIndex,
                    EmergencyWaterResupplyReadinessIndex: message.EmergencyWater.ResupplyReadinessIndex,
                    EmergencyWaterShortageRiskIndex: message.EmergencyWater.ShortageRiskIndex,
                    EffectiveTickId: message.EffectiveTickId,
                    EffectiveAtUtc: message.EffectiveAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case SyncCityResourceSupplyStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city resource supply snapshot for cityId={CityId}, effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}, supplyStress={SupplyStress}.",
                        message.CityId,
                        result.EffectiveTickId,
                        result.EffectiveAtUtc,
                        result.SupplyStressIndex);
                    break;

                case SyncCityResourceSupplyStatus.Deferred:
                    logger.LogDebug(
                        message:
                        "Deferred classic city resource supply snapshot for cityId={CityId} until sim-time reaches effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        result.EffectiveTickId,
                        result.EffectiveAtUtc);
                    break;

                case SyncCityResourceSupplyStatus.Stale:
                    logger.LogWarning(
                        message:
                        "Skipped stale classic city resource supply snapshot for cityId={CityId}, effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        message.EffectiveTickId,
                        message.EffectiveAtUtc);
                    break;

                case SyncCityResourceSupplyStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city resource supply snapshot for cityId={CityId} because environmental state is not initialized yet.",
                        message.CityId);
                    break;

                case SyncCityResourceSupplyStatus.Concurrent:
                    logger.LogDebug(
                        message:
                        "Skipped classic city resource supply snapshot for cityId={CityId} after a concurrent update won the persistence race. Current effectiveTickId={EffectiveTickId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        result.EffectiveTickId,
                        result.EffectiveAtUtc);
                    break;
            }
        }
    }
}
