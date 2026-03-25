using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SeedCityEnvironmentalConditions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumer(
        IMediator mediator,
        ILogger<CityCreatedConsumer> logger) : IConsumer<CityCreatedV1>
    {
        public async Task Consume(ConsumeContext<CityCreatedV1> context)
        {
            CityCreatedV1 message = context.Message;

            if (!ClassicCityScenario.IsMatch(message.SimulationKind))
            {
                logger.LogDebug(
                    message: "Ignored city-created event for cityId={CityId} because simulationKind={SimulationKind} does not match ClassicCity.",
                    message.CityId,
                    message.SimulationKind);
                return;
            }

            SeedCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: message.CityId,
                    CreatedAtUtc: message.CreatedAtUtc,
                    SimulationKind: message.SimulationKind,
                    DevelopmentLevel: message.DevelopmentLevel),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case SeedCityEnvironmentalConditionsStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Initialized classic city environmental state for cityId={CityId}, simulationKind={SimulationKind}.",
                        message.CityId,
                        message.SimulationKind);
                    break;

                case SeedCityEnvironmentalConditionsStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate classic city environmental seed for cityId={CityId}.",
                        message.CityId);
                    break;

                case SeedCityEnvironmentalConditionsStatus.IgnoredSimulationKind:
                    logger.LogDebug(
                        message: "Skipped classic city environmental seed for cityId={CityId} because simulationKind={SimulationKind} is not handled by this scenario.",
                        message.CityId,
                        message.SimulationKind);
                    break;
            }
        }
    }
}
