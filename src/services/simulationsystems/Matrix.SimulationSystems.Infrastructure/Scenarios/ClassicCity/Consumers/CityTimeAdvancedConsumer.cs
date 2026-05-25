using MassTransit;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<CityTickPhaseReachedV1>
    {
        public Task Consume(ConsumeContext<CityTickPhaseReachedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityTickPhaseReachedV1 message,
            CancellationToken cancellationToken)
        {
            if (message.TickContext.Phase != CityTickPhaseV1.SystemsDegradation)
                return;

            AdvanceCityEnvironmentalConditionsResult result = await mediator.Send(
                request: new AdvanceCityEnvironmentalConditionsCommand(
                    CityId: message.CityId,
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
                        message.CityId,
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
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityEnvironmentalConditionsStatus.OutOfOrder:
                    logger.LogDebug(
                        message:
                        "Skipped out-of-order classic city environmental time progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityEnvironmentalConditionsStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city environmental time progression for cityId={CityId}, tickId={TickId} because state is not initialized yet.",
                        message.CityId,
                        message.TickId);
                    break;
            }
        }
    }
}
