using MassTransit;
using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentHealthRiskV2Consumer(
        IMediator mediator,
        PatientHealthcareSupportPolicy healthcareSupportPolicy,
        PatientEnvironmentalHealthPolicy environmentalHealthPolicy,
        ILogger<PopulationResidentHealthRiskV2Consumer> logger)
        : IConsumer<PopulationResidentHealthRiskBatchV2>
    {
        public Task Consume(ConsumeContext<PopulationResidentHealthRiskBatchV2> context)
        {
            return ConsumeAsync(context.Message, context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            PopulationResidentHealthRiskBatchV2 message,
            CancellationToken cancellationToken)
        {
            AdvancePatientHealthCommand command = PopulationResidentHealthRiskCommandMapper.Map(
                message,
                healthcareSupportPolicy,
                environmentalHealthPolicy);
            AdvancePatientHealthResult result = await mediator.Send(command, cancellationToken);

            if (result.Status == AdvancePatientHealthStatus.SimulationDeleted)
            {
                logger.LogDebug(
                    "Ignored raw resident health risks for deleted healthcare simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    message.CorrelationId);
                return;
            }

            logger.LogInformation(
                "Advanced healthcare patients from raw health context for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, processed={ProcessedPatients}, ignored={IgnoredPatients}, stale={StalePatients}, outcomes={OutcomeCount}, batchSetComplete={BatchSetComplete}, completedBatchSetNow={CompletedBatchSetNow}, careAssignmentsCreated={CareAssignmentsCreated}, careAssignmentsDelivered={CareAssignmentsDelivered}, careAssignmentsCancelled={CareAssignmentsCancelled}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.BatchNumber,
                message.TotalBatches,
                result.ProcessedPatients,
                result.IgnoredPatients,
                result.StalePatients,
                result.Outcomes.Count,
                result.IsBatchSetComplete,
                result.CompletedBatchSetNow,
                result.CareAssignmentsCreated,
                result.CareAssignmentsDelivered,
                result.CareAssignmentsCancelled,
                message.CorrelationId);
        }
    }
}
