using MassTransit;
using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Education.Integration.Consumers
{
    public sealed class PopulationResidentFactsConsumer(
        IMediator mediator,
        ILogger<PopulationResidentFactsConsumer> logger)
        : IConsumer<PopulationResidentFactsBatchV1>
    {
        public Task Consume(ConsumeContext<PopulationResidentFactsBatchV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            PopulationResidentFactsBatchV1 message,
            CancellationToken cancellationToken)
        {
            SynchronizeStudentProfilesCommand command = PopulationResidentFactsCommandMapper.Map(message);
            SynchronizeStudentProfilesResult result = await mediator.Send(
                request: command,
                cancellationToken: cancellationToken);

            if (result.Status == SynchronizeStudentProfilesStatus.SimulationDeleted)
            {
                logger.LogDebug(
                    message: "Ignored population resident facts for deleted simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    message.CorrelationId);
                return;
            }

            logger.LogInformation(
                message: "Synchronized education resident profiles for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, added={AddedProfiles}, updated={UpdatedProfiles}, ignored={IgnoredProfiles}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.BatchNumber,
                message.TotalBatches,
                result.AddedProfiles,
                result.UpdatedProfiles,
                result.IgnoredProfiles,
                message.CorrelationId);
        }
    }
}
