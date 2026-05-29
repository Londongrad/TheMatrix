using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumer(
        IMediator mediator,
        ILogger<CityDeletedConsumer> logger) : IConsumer<SimulationDeletedV1>
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

            DeleteCitySystemsDataResult result = await mediator.Send(
                request: new DeleteCitySystemsDataCommand(
                    CityId: message.HostId,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteCitySystemsDataStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted simulation systems data for cityId={CityId}.",
                        message.HostId);
                    break;
                case DeleteCitySystemsDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate simulation systems deletion for cityId={CityId}.",
                        message.HostId);
                    break;
                case DeleteCitySystemsDataStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale simulation systems deletion for cityId={CityId}.",
                        message.HostId);
                    break;
            }
        }
    }
}
