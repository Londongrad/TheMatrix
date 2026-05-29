using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<SimulationTickPhaseReachedV1>
    {
        public Task Consume(ConsumeContext<SimulationTickPhaseReachedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationTickPhaseReachedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey) ||
                !string.Equals(
                    message.PhaseKey,
                    ClassicCityTickPhaseKeys.SystemsDegradation,
                    StringComparison.Ordinal))
                return;

            AdvanceCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new AdvanceCityEnvironmentalConditionsCommand(
                    CityId: message.HostId,
                    FromSimTimeUtc: message.FromSimTimeUtc,
                    ToSimTimeUtc: message.ToSimTimeUtc,
                    TickId: message.TickId),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case AdvanceCityEnvironmentalConditionsStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city environmental time progression for cityId={CityId}, tickId={TickId}, processedSimMinutes={ProcessedSimMinutes}, flooding={Flooding}, snow={Snow}, roadAccessibility={RoadAccessibility}.",
                        message.HostId,
                        message.TickId,
                        result.ProcessedSimMinutes,
                        result.FloodingIndex,
                        result.SnowAccumulationIndex,
                        result.RoadAccessibilityIndex);
                    break;

                case AdvanceCityEnvironmentalConditionsStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city environmental time progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;

                case AdvanceCityEnvironmentalConditionsStatus.OutOfOrder:
                    logger.LogDebug(
                        message:
                        "Skipped out-of-order classic city environmental time progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;

                case AdvanceCityEnvironmentalConditionsStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city environmental time progression for cityId={CityId}, tickId={TickId} because state is not initialized yet.",
                        message.HostId,
                        message.TickId);
                    break;
            }
        }
    }
}
