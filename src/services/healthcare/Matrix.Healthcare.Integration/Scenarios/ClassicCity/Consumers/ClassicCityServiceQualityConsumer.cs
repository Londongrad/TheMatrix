using MassTransit;
using Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;

public sealed class ClassicCityServiceQualityConsumer(
    IMediator mediator,
    ILogger<ClassicCityServiceQualityConsumer> logger)
    : IConsumer<ClassicCityServiceQualitySnapshotV1>
{
    public Task Consume(ConsumeContext<ClassicCityServiceQualitySnapshotV1> context)
    {
        return ConsumeAsync(context.Message, context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        ClassicCityServiceQualitySnapshotV1 message,
        CancellationToken cancellationToken)
    {
        SynchronizeCareServiceQualityResult result = await mediator.Send(
            ClassicCityServiceQualityCommandMapper.Map(message),
            cancellationToken);

        if (result.Status == SynchronizeCareServiceQualityStatus.SimulationDeleted)
        {
            logger.LogDebug(
                "Ignored Classic City care quality for deleted simulationHostId={SimulationHostId}.",
                message.CityId);
            return;
        }

        logger.LogInformation(
            "Synchronized Classic City care quality for simulationHostId={SimulationHostId}, qualityMultiplier={QualityMultiplier}, created={StateCreated}, updated={StateUpdated}.",
            message.CityId,
            message.HealthcareQualityIndex,
            result.StateCreated,
            result.StateUpdated);
    }
}
