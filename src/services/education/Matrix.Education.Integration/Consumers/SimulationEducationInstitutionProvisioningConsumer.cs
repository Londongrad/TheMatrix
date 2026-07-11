using MassTransit;
using Matrix.Education.Application.Institutions.SynchronizeEducationInstitutions;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Education.Integration.Consumers;

public sealed class SimulationEducationInstitutionProvisioningConsumer(
    IMediator mediator,
    ILogger<SimulationEducationInstitutionProvisioningConsumer> logger)
    : IConsumer<SimulationEducationInstitutionProvisioningBatchV1>
{
    public Task Consume(ConsumeContext<SimulationEducationInstitutionProvisioningBatchV1> context)
    {
        return ConsumeAsync(
            message: context.Message,
            cancellationToken: context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        SimulationEducationInstitutionProvisioningBatchV1 message,
        CancellationToken cancellationToken)
    {
        SynchronizeEducationInstitutionsCommand command =
            SimulationEducationInstitutionProvisioningCommandMapper.Map(message);
        SynchronizeEducationInstitutionsResult result = await mediator.Send(
            request: command,
            cancellationToken: cancellationToken);

        if (result.Status == SynchronizeEducationInstitutionsStatus.SimulationDeleted)
        {
            logger.LogDebug(
                message: "Ignored education institution provisioning for deleted simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.CorrelationId);
            return;
        }

        logger.LogInformation(
            message: "Synchronized education institutions for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, added={AddedInstitutions}, updated={UpdatedInstitutions}, ignored={IgnoredInstitutions}, correlationId={CorrelationId}.",
            message.SimulationHostId,
            message.SourceRevision,
            message.BatchNumber,
            message.TotalBatches,
            result.AddedInstitutions,
            result.UpdatedInstitutions,
            result.IgnoredInstitutions,
            message.CorrelationId);
    }
}
