using MassTransit;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentHealthRiskConsumer(
        IMediator mediator,
        ILogger<PopulationResidentHealthRiskConsumer> logger)
        : IConsumer<PopulationResidentHealthRiskBatchV1>
    {
        public Task Consume(ConsumeContext<PopulationResidentHealthRiskBatchV1> context)
        {
            return ConsumeAsync(context.Message, context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            PopulationResidentHealthRiskBatchV1 message,
            CancellationToken cancellationToken)
        {
            AdvancePatientHealthCommand command = PopulationResidentHealthRiskCommandMapper.Map(message);
            AdvancePatientHealthResult result = await mediator.Send(command, cancellationToken);

            if (result.Status == AdvancePatientHealthStatus.SimulationDeleted)
            {
                logger.LogDebug(
                    "Ignored resident health risks for deleted healthcare simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    message.CorrelationId);
                return;
            }

            logger.LogInformation(
                "Advanced healthcare patients for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, processed={ProcessedPatients}, ignored={IgnoredPatients}, stale={StalePatients}, outcomes={OutcomeCount}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.BatchNumber,
                message.TotalBatches,
                result.ProcessedPatients,
                result.IgnoredPatients,
                result.StalePatients,
                result.Outcomes.Count,
                message.CorrelationId);
        }
    }
}
