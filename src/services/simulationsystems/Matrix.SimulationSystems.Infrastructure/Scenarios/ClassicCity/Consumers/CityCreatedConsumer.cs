using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
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

            SeedCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new SeedCityEnvironmentalConditionsCommand(
                    CityId: message.CityId,
                    CreatedAtUtc: message.CreatedAtUtc,
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
            }
        }
    }
}
