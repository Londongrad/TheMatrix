using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityCreatedConsumer(
        IMediator mediator,
        ILogger<CityCreatedConsumer> logger) : IConsumer<CityCreatedV1>
    {
        public Task Consume(ConsumeContext<CityCreatedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityCreatedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityScenario.IsMatch(message.SimulationKind))
            {
                logger.LogDebug(
                    message:
                    "Ignored city-created event for cityId={CityId} because simulationKind={SimulationKind} does not match ClassicCity.",
                    message.CityId,
                    message.SimulationKind);
                return;
            }

            SeedCityStockpilesResult result = await mediator.Send(
                request: new SeedCityStockpilesCommand(
                    CityId: message.CityId,
                    CreatedAtUtc: message.CreatedAtUtc,
                    SimulationKind: message.SimulationKind,
                    DevelopmentLevel: message.DevelopmentLevel),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case SeedCityStockpilesStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Initialized classic city stockpiles for cityId={CityId}, supplyStress={SupplyStress}.",
                        message.CityId,
                        result.SupplyStressIndex);
                    break;

                case SeedCityStockpilesStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate classic city stockpile seed for cityId={CityId}.",
                        message.CityId);
                    break;

                case SeedCityStockpilesStatus.IgnoredSimulationKind:
                    logger.LogDebug(
                        message:
                        "Skipped classic city stockpile seed for cityId={CityId} because simulationKind={SimulationKind} is not handled by this scenario.",
                        message.CityId,
                        message.SimulationKind);
                    break;
            }
        }
    }
}
