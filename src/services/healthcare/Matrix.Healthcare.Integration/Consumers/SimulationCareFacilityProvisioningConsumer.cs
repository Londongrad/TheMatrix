using MassTransit;
using Matrix.Healthcare.Application.Facilities.SynchronizeCareFacilities;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers;

public sealed class SimulationCareFacilityProvisioningConsumer(
    IMediator mediator,
    ILogger<SimulationCareFacilityProvisioningConsumer> logger)
    : IConsumer<SimulationCareFacilityProvisioningBatchV1>
{
    public Task Consume(ConsumeContext<SimulationCareFacilityProvisioningBatchV1> context)
    {
        return ConsumeAsync(
            message: context.Message,
            cancellationToken: context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        SimulationCareFacilityProvisioningBatchV1 message,
        CancellationToken cancellationToken)
    {
        SynchronizeCareFacilitiesCommand command =
            SimulationCareFacilityProvisioningCommandMapper.Map(message);
        SynchronizeCareFacilitiesResult result = await mediator.Send(
            request: command,
            cancellationToken: cancellationToken);

        if (result.Status == SynchronizeCareFacilitiesStatus.SimulationDeleted)
        {
            logger.LogDebug(
                message: "Ignored care facility provisioning for deleted healthcare simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.CorrelationId);
            return;
        }

        logger.LogInformation(
            message: "Synchronized healthcare facilities for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, added={AddedFacilities}, updated={UpdatedFacilities}, ignored={IgnoredFacilities}, correlationId={CorrelationId}.",
            message.SimulationHostId,
            message.SourceRevision,
            message.BatchNumber,
            message.TotalBatches,
            result.AddedFacilities,
            result.UpdatedFacilities,
            result.IgnoredFacilities,
            message.CorrelationId);
    }
}
