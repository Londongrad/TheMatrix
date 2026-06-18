using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityWeatherCreatedConsumer(
        IMediator mediator,
        ILogger<CityWeatherCreatedConsumer> logger) : IConsumer<CityWeatherCreatedV1>
    {
        public Task Consume(ConsumeContext<CityWeatherCreatedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityWeatherCreatedV1 message,
            CancellationToken cancellationToken)
        {
            RecalculateCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new RecalculateCityEnvironmentalConditionsCommand(
                    CityId: message.CityId,
                    AtSimTimeUtc: message.AtSimTimeUtc,
                    Weather: new CityWeatherSystemInput(
                        Type: message.InitialState.Type,
                        Severity: message.InitialState.Severity,
                        PrecipitationKind: message.InitialState.PrecipitationKind,
                        TemperatureC: message.InitialState.TemperatureC,
                        HumidityPercent: message.InitialState.HumidityPercent,
                        WindSpeedKph: message.InitialState.WindSpeedKph,
                        CloudCoveragePercent: message.InitialState.CloudCoveragePercent,
                        PressureHpa: message.InitialState.PressureHpa)),
                cancellationToken: cancellationToken);

            LogResult(
                logger: logger,
                cityId: message.CityId,
                result: result,
                operation: "weather-initialization");
        }

        private static void LogResult(
            ILogger logger,
            Guid cityId,
            RecalculateCityEnvironmentalConditionsResult result,
            string operation)
        {
            switch (result.Status)
            {
                case RecalculateCityEnvironmentalConditionsStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city environmental {Operation} for cityId={CityId}, flooding={Flooding}, snow={Snow}, roadAccessibility={RoadAccessibility}.",
                        operation,
                        cityId,
                        result.FloodingIndex,
                        result.SnowAccumulationIndex,
                        result.RoadAccessibilityIndex);
                    break;

                case RecalculateCityEnvironmentalConditionsStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city environmental {Operation} for cityId={CityId}.",
                        operation,
                        cityId);
                    break;

                case RecalculateCityEnvironmentalConditionsStatus.Stale:
                    logger.LogWarning(
                        message: "Skipped stale classic city environmental {Operation} for cityId={CityId}.",
                        operation,
                        cityId);
                    break;

                case RecalculateCityEnvironmentalConditionsStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city environmental {Operation} for cityId={CityId} because state is not initialized yet.",
                        operation,
                        cityId);
                    break;
            }
        }
    }
}
