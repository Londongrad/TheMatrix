using MassTransit;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;

public sealed class SimulationTickPhaseReachedConsumer(
    IMediator mediator,
    ILogger<SimulationTickPhaseReachedConsumer> logger) : IConsumer<SimulationTickPhaseReachedV1>
{
    public Task Consume(ConsumeContext<SimulationTickPhaseReachedV1> context)
    {
        return ConsumeAsync(context.Message, context.CancellationToken);
    }

    internal async Task ConsumeAsync(
        SimulationTickPhaseReachedV1 message,
        CancellationToken cancellationToken)
    {
        if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey) ||
            !string.Equals(
                message.PhaseKey,
                ClassicCityTickPhaseKeys.ResourceSettlement,
                StringComparison.Ordinal))
            return;

        AdvanceCityStockpilesResult result = await mediator.Send(
            request: new AdvanceCityStockpilesCommand(
                CityId: message.HostId,
                FromSimTimeUtc: message.FromSimTimeUtc,
                ToSimTimeUtc: message.ToSimTimeUtc,
                TickId: message.TickId),
            cancellationToken: cancellationToken);

        switch (result.Status)
        {
            case AdvanceCityStockpilesStatus.Applied:
                logger.LogInformation(
                    "Applied classic city stockpile time progression for cityId={CityId}, tickId={TickId}, processedSimMinutes={ProcessedSimMinutes}, supplyStress={SupplyStress}.",
                    message.HostId,
                    message.TickId,
                    result.ProcessedSimMinutes,
                    result.SupplyStressIndex);
                break;
            case AdvanceCityStockpilesStatus.Duplicate:
                logger.LogDebug(
                    "Skipped duplicate classic city stockpile time progression for cityId={CityId}, tickId={TickId}.",
                    message.HostId,
                    message.TickId);
                break;
            case AdvanceCityStockpilesStatus.OutOfOrder:
                logger.LogDebug(
                    "Skipped out-of-order classic city stockpile time progression for cityId={CityId}, tickId={TickId}.",
                    message.HostId,
                    message.TickId);
                break;
            case AdvanceCityStockpilesStatus.NotInitialized:
                logger.LogDebug(
                    "Skipped classic city stockpile time progression for cityId={CityId}, tickId={TickId} because state is not initialized yet.",
                    message.HostId,
                    message.TickId);
                break;
        }
    }
}
