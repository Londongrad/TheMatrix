using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CitySystemsResourceDemandConsumer(
        IMediator mediator,
        ILogger<CitySystemsResourceDemandConsumer> logger) : IConsumer<ClassicCitySystemsResourceDemandSnapshotV1>
    {
        public async Task Consume(ConsumeContext<ClassicCitySystemsResourceDemandSnapshotV1> context)
        {
            ClassicCitySystemsResourceDemandSnapshotV1 message = context.Message;

            SyncCitySystemsDemandResult result = await mediator.Send(
                request: new SyncCitySystemsDemandCommand(
                    CityId: message.CityId,
                    FuelDemandPressureIndex: message.FuelDemandPressureIndex,
                    SparePartsDemandPressureIndex: message.SparePartsDemandPressureIndex,
                    FiltersDemandPressureIndex: message.FiltersDemandPressureIndex,
                    EmergencyWaterDemandPressureIndex: message.EmergencyWaterDemandPressureIndex,
                    OverallDemandPressureIndex: message.OverallDemandPressureIndex,
                    EffectiveAtUtc: message.EffectiveAtUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case SyncCitySystemsDemandStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city systems resource demand for cityId={CityId}, effectiveAtUtc={EffectiveAtUtc}, overallDemand={OverallDemand}.",
                        message.CityId,
                        result.EffectiveAtUtc,
                        result.OverallDemandPressureIndex);
                    break;

                case SyncCitySystemsDemandStatus.Deferred:
                    logger.LogDebug(
                        message:
                        "Deferred classic city systems resource demand for cityId={CityId} until stockpiles reach effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        result.EffectiveAtUtc);
                    break;

                case SyncCitySystemsDemandStatus.Stale:
                    logger.LogWarning(
                        message:
                        "Skipped stale classic city systems resource demand for cityId={CityId}, effectiveAtUtc={EffectiveAtUtc}.",
                        message.CityId,
                        message.EffectiveAtUtc);
                    break;

                case SyncCitySystemsDemandStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city systems resource demand for cityId={CityId} because stockpiles are not initialized yet.",
                        message.CityId);
                    break;
            }
        }
    }
}
