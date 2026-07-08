using MassTransit;
using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class HealthcarePopulationHealthSnapshotConsumer(
    IMediator mediator,
    ILogger<HealthcarePopulationHealthSnapshotConsumer> logger)
    : IConsumer<HealthcarePopulationHealthSnapshotV1>
{
    public Task Consume(ConsumeContext<HealthcarePopulationHealthSnapshotV1> context)
    {
        return ConsumeAsync(
            context.Message,
            context.MessageId,
            context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        HealthcarePopulationHealthSnapshotV1 message,
        Guid? messageId,
        CancellationToken cancellationToken)
    {
        if (messageId is null)
            throw new InvalidOperationException(
                "HealthcarePopulationHealthSnapshot message must have a MessageId.");

        ApplyHealthcarePressureSnapshotResult result = await mediator.Send(
            HealthcarePopulationHealthSnapshotCommandMapper.Map(
                message,
                messageId.Value,
                HealthcarePopulationHealthSnapshotConsumerDefinition.EndpointNameValue),
            cancellationToken);

        if (result.Status == ApplyHealthcarePressureSnapshotStatus.Applied)
            logger.LogInformation(
                "Applied healthcare population snapshot for cityId={CityId}, revision={Revision}, patients={Patients}, activeIllnesses={ActiveIllnesses}.",
                message.SimulationHostId,
                message.SourceRevision,
                message.PatientCount,
                message.ActiveIllnessCount);
        else
            logger.LogDebug(
                "Skipped healthcare population snapshot for cityId={CityId}, revision={Revision}, status={Status}.",
                message.SimulationHostId,
                message.SourceRevision,
                result.Status);
    }
}
