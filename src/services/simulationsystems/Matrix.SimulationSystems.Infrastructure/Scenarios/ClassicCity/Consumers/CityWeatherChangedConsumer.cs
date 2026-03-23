using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherChangedConsumer(
        IMediator mediator,
        ILogger<CityWeatherChangedConsumer> logger) : IConsumer<CityWeatherChangedV1>
    {
        public async Task Consume(ConsumeContext<CityWeatherChangedV1> context)
        {
            CityWeatherChangedV1 message = context.Message;

            RecalculateCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new RecalculateCityEnvironmentalConditionsCommand(
                    CityId: message.CityId,
                    AtSimTimeUtc: message.AtSimTimeUtc,
                    Weather: new CityWeatherSystemInput(
                        Type: message.CurrentState.Type,
                        Severity: message.CurrentState.Severity,
                        PrecipitationKind: message.CurrentState.PrecipitationKind,
                        TemperatureC: message.CurrentState.TemperatureC,
                        HumidityPercent: message.CurrentState.HumidityPercent,
                        WindSpeedKph: message.CurrentState.WindSpeedKph,
                        CloudCoveragePercent: message.CurrentState.CloudCoveragePercent,
                        PressureHpa: message.CurrentState.PressureHpa)),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case RecalculateCityEnvironmentalConditionsStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city environmental weather sync for cityId={CityId}, flooding={Flooding}, snow={Snow}, roadAccessibility={RoadAccessibility}.",
                        message.CityId,
                        result.FloodingIndex,
                        result.SnowAccumulationIndex,
                        result.RoadAccessibilityIndex);
                    break;

                case RecalculateCityEnvironmentalConditionsStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city environmental weather sync for cityId={CityId}.",
                        message.CityId);
                    break;

                case RecalculateCityEnvironmentalConditionsStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale classic city environmental weather sync for cityId={CityId}.",
                        message.CityId);
                    break;

                case RecalculateCityEnvironmentalConditionsStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city environmental weather sync for cityId={CityId} because state is not initialized yet.",
                        message.CityId);
                    break;
            }
        }
    }
}
