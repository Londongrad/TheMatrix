using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumer(
        IMediator mediator,
        ILogger<CityTimeAdvancedConsumer> logger) : IConsumer<CityTimeAdvancedV1>
    {
        public async Task Consume(ConsumeContext<CityTimeAdvancedV1> context)
        {
            CityTimeAdvancedV1 message = context.Message;

            AdvanceCityStockpilesResult result = await mediator.Send(
                request: new AdvanceCityStockpilesCommand(
                    CityId: message.CityId,
                    FromSimTimeUtc: message.FromSimTimeUtc,
                    ToSimTimeUtc: message.ToSimTimeUtc),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case AdvanceCityStockpilesStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city stockpile time progression for cityId={CityId}, tickId={TickId}, processedSimMinutes={ProcessedSimMinutes}, supplyStress={SupplyStress}.",
                        message.CityId,
                        message.TickId,
                        result.ProcessedSimMinutes,
                        result.SupplyStressIndex);
                    break;

                case AdvanceCityStockpilesStatus.Duplicate:
                    logger.LogDebug(
                        message: "Skipped duplicate classic city stockpile time progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityStockpilesStatus.OutOfOrder:
                    logger.LogWarning(
                        message: "Skipped out-of-order classic city stockpile time progression for cityId={CityId}, tickId={TickId}.",
                        message.CityId,
                        message.TickId);
                    break;

                case AdvanceCityStockpilesStatus.NotInitialized:
                    logger.LogDebug(
                        message:
                        "Skipped classic city stockpile time progression for cityId={CityId}, tickId={TickId} because state is not initialized yet.",
                        message.CityId,
                        message.TickId);
                    break;
            }
        }
    }
}
