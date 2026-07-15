using MassTransit;
using Matrix.Education.Contracts.Events;
using Matrix.Population.Application.Integration.Education.ApplyEducationParticipation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Consumers.Education
{
    public sealed class EducationStudentParticipationConsumer(
        IMediator mediator,
        ILogger<EducationStudentParticipationConsumer> logger)
        : IConsumer<EducationStudentParticipationBatchV1>
    {
        public Task Consume(ConsumeContext<EducationStudentParticipationBatchV1> context)
        {
            return ConsumeAsync(
                context.Message,
                context.MessageId,
                context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            EducationStudentParticipationBatchV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException(
                    "EducationStudentParticipation message must have a MessageId.");

            ApplyEducationParticipationResult result = await mediator.Send(
                EducationStudentParticipationCommandMapper.Map(
                    message,
                    messageId.Value,
                    EducationStudentParticipationConsumerDefinition.EndpointNameValue),
                cancellationToken);

            if (result.Status == ApplyEducationParticipationStatus.Applied)
                logger.LogInformation(
                    "Applied education participation for simulationHostId={SimulationHostId}, applied={Applied}, stale={Stale}, missing={Missing}, batch={BatchNumber}/{TotalBatches}.",
                    message.SimulationHostId,
                    result.AppliedStudentCount,
                    result.StaleStudentCount,
                    result.MissingOrChangedResidentCount,
                    message.BatchNumber,
                    message.TotalBatches);
            else
                logger.LogDebug(
                    "Skipped education participation for simulationHostId={SimulationHostId}, status={Status}, batch={BatchNumber}/{TotalBatches}.",
                    message.SimulationHostId,
                    result.Status,
                    message.BatchNumber,
                    message.TotalBatches);
        }
    }
}
