using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class SimulationDeletedConsumer(
    IMediator mediator,
    ILogger<SimulationDeletedConsumer> logger) : IConsumer<SimulationDeletedV1>
{
    public Task Consume(ConsumeContext<SimulationDeletedV1> context)
    {
        return ConsumeAsync(
            message: context.Message,
            cancellationToken: context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        SimulationDeletedV1 message,
        CancellationToken cancellationToken)
    {
        if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey))
        {
            logger.LogDebug(
                "Ignored simulation deletion for simulationId={SimulationId}, scenarioKey={ScenarioKey}, hostTypeKey={HostTypeKey}.",
                message.SimulationId,
                message.ScenarioKey,
                message.HostTypeKey);
            return;
        }

        DeleteCityResourcesResult result = await mediator.Send(
            request: new DeleteCityResourcesCommand(
                CityId: message.HostId,
                DeletedAtUtc: message.DeletedAtUtc),
            cancellationToken: cancellationToken);

        switch (result.Status)
        {
            case DeleteCityResourcesStatus.Applied:
                logger.LogInformation("Deleted resource data for cityId={CityId}.", message.HostId);
                break;
            case DeleteCityResourcesStatus.Duplicate:
                logger.LogDebug("Skipped duplicate resource deletion for cityId={CityId}.", message.HostId);
                break;
            case DeleteCityResourcesStatus.Stale:
                logger.LogWarning("Ignored stale resource deletion for cityId={CityId}.", message.HostId);
                break;
        }
    }
}
