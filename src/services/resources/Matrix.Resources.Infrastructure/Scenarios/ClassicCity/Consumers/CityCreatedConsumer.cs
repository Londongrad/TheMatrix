using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
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

            SeedCityStockpilesResult result = await mediator.Send(
                request: new SeedCityStockpilesCommand(
                    CityId: message.HostId,
                    CreatedAtUtc: message.CreatedAtUtc,
                    SimulationKind: ClassicCityScenario.Name,
                    DevelopmentLevel: message.DevelopmentLevel),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case SeedCityStockpilesStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Initialized classic city stockpiles for cityId={CityId}, supplyStress={SupplyStress}.",
                        message.HostId,
                        result.SupplyStressIndex);
                    break;

                case SeedCityStockpilesStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate classic city stockpile seed for cityId={CityId}.",
                        message.HostId);
                    break;

                case SeedCityStockpilesStatus.IgnoredSimulationKind:
                    logger.LogDebug(
                        message:
                        "Skipped classic city stockpile seed for cityId={CityId} because simulationKind={SimulationKind} is not handled by this scenario.",
                        message.HostId,
                        ClassicCityScenario.Name);
                    break;

                case SeedCityStockpilesStatus.CityDeleted:
                    logger.LogWarning(
                        message: "Ignored stockpile initialization for deleted cityId={CityId}.",
                        message.HostId);
                    break;
            }
        }
    }
}
