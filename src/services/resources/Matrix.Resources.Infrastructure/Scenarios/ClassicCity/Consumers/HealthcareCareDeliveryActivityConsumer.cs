using MassTransit;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class HealthcareCareDeliveryActivityConsumer(
    IMediator mediator,
    ILogger<HealthcareCareDeliveryActivityConsumer> logger)
    : IConsumer<HealthcareCareDeliveryActivityV1>
{
    public Task Consume(ConsumeContext<HealthcareCareDeliveryActivityV1> context)
    {
        return ConsumeAsync(context.Message, context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        HealthcareCareDeliveryActivityV1 message,
        CancellationToken cancellationToken)
    {
        ApplyCityHealthcareMedicineDemandResult result = await mediator.Send(
            new ApplyCityHealthcareMedicineDemandCommand(
                CityId: message.SimulationHostId,
                ProcessedPatientCount: message.ProcessedPatientCount,
                RoutineCareDeliveryCount: message.RoutineCareDeliveryCount,
                UrgentCareDeliveryCount: message.UrgentCareDeliveryCount,
                AcuteCareDeliveryCount: message.AcuteCareDeliveryCount,
                EmergencyCareDeliveryCount: message.EmergencyCareDeliveryCount,
                SourceRevision: message.SourceRevision,
                CareDate: message.CareDate,
                ObservedAtUtc: message.OccurredAtUtc),
            cancellationToken);

        switch (result.Status)
        {
            case ApplyCityHealthcareMedicineDemandStatus.Applied:
                logger.LogInformation(
                    "Applied healthcare medicine demand for cityId={CityId}, sourceRevision={SourceRevision}, medicineLoad={MedicineLoad}, medicineStock={MedicineStock}.",
                    message.SimulationHostId,
                    result.SourceRevision,
                    result.MedicineLoadIndex,
                    result.MedicineStockLevelIndex);
                break;
            case ApplyCityHealthcareMedicineDemandStatus.Duplicate:
                logger.LogDebug(
                    "Skipped duplicate healthcare medicine demand for cityId={CityId}, sourceRevision={SourceRevision}.",
                    message.SimulationHostId,
                    message.SourceRevision);
                break;
            case ApplyCityHealthcareMedicineDemandStatus.Stale:
                logger.LogWarning(
                    "Skipped stale healthcare medicine demand for cityId={CityId}, sourceRevision={SourceRevision}, currentRevision={CurrentRevision}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    result.SourceRevision);
                break;
            case ApplyCityHealthcareMedicineDemandStatus.NotInitialized:
                logger.LogDebug(
                    "Skipped healthcare medicine demand for cityId={CityId} because stockpiles are not initialized yet.",
                    message.SimulationHostId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result));
        }
    }
}
