using MassTransit;
using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Population.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Consumers
{
    public sealed class PopulationResidentMedicalStateConsumer(
        IMediator mediator,
        ILogger<PopulationResidentMedicalStateConsumer> logger)
        : IConsumer<PopulationResidentMedicalStateBatchV1>
    {
        public Task Consume(ConsumeContext<PopulationResidentMedicalStateBatchV1> context)
        {
            return ConsumeAsync(context.Message, context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            PopulationResidentMedicalStateBatchV1 message,
            CancellationToken cancellationToken)
        {
            InitializePatientMedicalRecordsCommand command =
                PopulationResidentMedicalStateCommandMapper.Map(message);
            InitializePatientMedicalRecordsResult result = await mediator.Send(
                request: command,
                cancellationToken: cancellationToken);

            if (result.Status == InitializePatientMedicalRecordsStatus.SimulationDeleted)
            {
                logger.LogDebug(
                    message: "Ignored medical state for deleted healthcare simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, correlationId={CorrelationId}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    message.CorrelationId);
                return;
            }

            logger.LogInformation(
                message: "Initialized healthcare medical records for simulationHostId={SimulationHostId}, sourceRevision={SourceRevision}, batch={BatchNumber}/{TotalBatches}, added={AddedRecords}, ignored={IgnoredRecords}, correlationId={CorrelationId}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.BatchNumber,
                message.TotalBatches,
                result.AddedRecords,
                result.IgnoredRecords,
                message.CorrelationId);
        }
    }
}
