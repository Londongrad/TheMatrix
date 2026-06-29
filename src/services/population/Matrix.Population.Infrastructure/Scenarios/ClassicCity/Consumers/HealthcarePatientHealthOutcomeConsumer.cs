using MassTransit;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class HealthcarePatientHealthOutcomeConsumer(
        IMediator mediator,
        ILogger<HealthcarePatientHealthOutcomeConsumer> logger)
        : IConsumer<HealthcarePatientHealthOutcomeBatchV1>
    {
        public Task Consume(ConsumeContext<HealthcarePatientHealthOutcomeBatchV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            HealthcarePatientHealthOutcomeBatchV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException(
                    "HealthcarePatientHealthOutcome message must have a MessageId.");

            ApplyPatientHealthOutcomesResult result = await mediator.Send(
                HealthcarePatientHealthOutcomeCommandMapper.Map(
                    message,
                    messageId.Value,
                    HealthcarePatientHealthOutcomeConsumerDefinition.EndpointNameValue),
                cancellationToken);

            if (result.Status == ApplyPatientHealthOutcomesStatus.Applied)
                logger.LogInformation(
                    "Applied healthcare outcomes for cityId={CityId}, revision={Revision}, patients={Patients}, stale={Stale}, batch={BatchNumber}/{TotalBatches}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    result.AppliedPatientCount,
                    result.StalePatientCount,
                    message.BatchNumber,
                    message.TotalBatches);
            else
                logger.LogDebug(
                    "Skipped healthcare outcomes for cityId={CityId}, revision={Revision}, status={Status}.",
                    message.SimulationHostId,
                    message.SourceRevision,
                    result.Status);
        }
    }
}
