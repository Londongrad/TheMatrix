using MassTransit;
using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentVitalStateConsumer(
        IMediator mediator,
        ILogger<PopulationResidentVitalStateConsumer> logger)
        : IConsumer<PopulationResidentVitalStateBatchV1>
    {
        public Task Consume(ConsumeContext<PopulationResidentVitalStateBatchV1> context)
        {
            return ConsumeAsync(context.Message, context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            PopulationResidentVitalStateBatchV1 message,
            CancellationToken cancellationToken)
        {
            InitializePatientMedicalRecordsCommand command =
                PopulationResidentVitalStateCommandMapper.Map(message);
            InitializePatientMedicalRecordsResult result = await mediator.Send(
                request: command,
                cancellationToken: cancellationToken);

            if (result.Status == InitializePatientMedicalRecordsStatus.SimulationDeleted)
            {
                logger.LogDebug(
                    message: "Ignored vital state for deleted healthcare simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    message.CorrelationId);
                return;
            }

            logger.LogInformation(
                message: "Synchronized patient vital state for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, added={AddedRecords}, updated={UpdatedRecords}, ignored={IgnoredRecords}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.BatchNumber,
                message.TotalBatches,
                result.AddedRecords,
                result.UpdatedRecords,
                result.IgnoredRecords,
                message.CorrelationId);
        }
    }
}
