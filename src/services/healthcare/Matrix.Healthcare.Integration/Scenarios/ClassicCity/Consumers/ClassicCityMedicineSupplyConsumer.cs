using MassTransit;
using Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Resources;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityMedicineSupplyConsumer(
    IMediator mediator,
    ILogger<ClassicCityMedicineSupplyConsumer> logger)
    : IConsumer<ClassicCityStockpileSnapshotV1>
{
    public Task Consume(ConsumeContext<ClassicCityStockpileSnapshotV1> context)
    {
        return ConsumeAsync(context.Message, context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        ClassicCityStockpileSnapshotV1 message,
        CancellationToken cancellationToken)
    {
        SynchronizeCareMedicineSupplyResult result = await mediator.Send(
            ClassicCityMedicineSupplyCommandMapper.Map(message),
            cancellationToken);

        if (result.Status == SynchronizeCareMedicineSupplyStatus.SimulationDeleted)
        {
            logger.LogDebug(
                "Ignored Classic City medicine supply for deleted simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}.",
                message.CityId,
                message.EffectiveTickId);
            return;
        }

        logger.LogInformation(
            "Synchronized Classic City medicine supply for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, stockLevel={StockLevel}, shortageRisk={ShortageRisk}, created={StateCreated}, updated={StateUpdated}.",
            message.CityId,
            message.EffectiveTickId,
            message.Medicine.StockLevelIndex,
            message.Medicine.ShortageRiskIndex,
            result.StateCreated,
            result.StateUpdated);
    }
}
