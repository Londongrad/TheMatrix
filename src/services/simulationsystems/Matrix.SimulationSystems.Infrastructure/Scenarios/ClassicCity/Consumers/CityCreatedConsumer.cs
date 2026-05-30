using MassTransit;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumer(
        IMediator mediator,
        ILogger<CityCreatedConsumer> logger) : IConsumer<ClassicCityCreatedV1>
    {
        public Task Consume(ConsumeContext<ClassicCityCreatedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityCreatedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey))
            {
                logger.LogDebug(
                    message:
                    "Ignored classic-city-created event for simulationId={SimulationId}, scenarioKey={ScenarioKey}, hostTypeKey={HostTypeKey}.",
                    message.SimulationId,
                    message.ScenarioKey,
                    message.HostTypeKey);
                return;
            }

            SeedCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: message.HostId,
                    CreatedAtUtc: message.CreatedAtUtc,
                    SimulationKind: ClassicCityScenario.Name,
                    DevelopmentLevel: message.DevelopmentLevel),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case SeedCityEnvironmentalConditionsStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Initialized classic city environmental state for cityId={CityId}, simulationKind={SimulationKind}.",
                        message.HostId,
                        ClassicCityScenario.Name);
                    break;

                case SeedCityEnvironmentalConditionsStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate classic city environmental seed for cityId={CityId}.",
                        message.HostId);
                    break;

                case SeedCityEnvironmentalConditionsStatus.IgnoredSimulationKind:
                    logger.LogDebug(
                        message:
                        "Skipped classic city environmental seed for cityId={CityId} because simulationKind={SimulationKind} is not handled by this scenario.",
                        message.HostId,
                        ClassicCityScenario.Name);
                    break;

                case SeedCityEnvironmentalConditionsStatus.CityDeleted:
                    logger.LogWarning(
                        message: "Ignored environmental initialization for deleted cityId={CityId}.",
                        message.HostId);
                    break;
            }
        }
    }
}
