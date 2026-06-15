using MassTransit;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Lifecycle.DeleteCityEconomyData;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDeletedConsumer(
        IMediator mediator,
        ILogger<CityDeletedConsumer> logger) : IConsumer<SimulationDeletedV1>
    {
        public Task Consume(ConsumeContext<SimulationDeletedV1> context)
        {
            return ConsumeAsync(context.Message, context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationDeletedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(
                    message.ScenarioKey,
                    message.HostTypeKey))
            {
                logger.LogDebug(
                    "Ignored simulation deletion for simulationId={SimulationId}, scenarioKey={ScenarioKey}, hostTypeKey={HostTypeKey}.",
                    message.SimulationId,
                    message.ScenarioKey,
                    message.HostTypeKey);
                return;
            }

            DeleteCityEconomyDataResult result = await mediator.Send(
                request: new DeleteCityEconomyDataCommand(
                    CityId: message.HostId,
                    DeletedAtUtc: message.DeletedAtUtc),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case DeleteCityEconomyDataStatus.Applied:
                    logger.LogInformation(
                        message: "Deleted economy data for cityId={CityId}.",
                        message.HostId);
                    break;
                case DeleteCityEconomyDataStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate economy deletion for cityId={CityId}.",
                        message.HostId);
                    break;
                case DeleteCityEconomyDataStatus.Stale:
                    logger.LogWarning(
                        message: "Ignored stale economy deletion for cityId={CityId}.",
                        message.HostId);
                    break;
            }
        }
    }
}
