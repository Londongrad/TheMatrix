using MassTransit;
using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers
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
            SynchronizePatientProfilesCommand command = PopulationResidentFactsCommandMapper.Map(message);
            SynchronizePatientProfilesResult result = await mediator.Send(
                request: command,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                message: "Synchronized healthcare patient profiles for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, added={AddedProfiles}, updated={UpdatedProfiles}, ignored={IgnoredProfiles}, correlationId={CorrelationId}.",
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
