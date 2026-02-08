using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Application.Services.Weather.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Simulation;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed class SimulationAdvanceExecutor(
        ISimulationClockRepository repository,
        IWeatherAdvanceExecutor weatherAdvanceExecutor,
        ICityCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : ISimulationAdvanceExecutor
    {
        public async Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(simulationId.Value);

            SimulationClock? clock = await repository.GetBySimulationIdAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            if (clock is null)
                return new SimulationAdvanceExecutionResult(
                    SimulationId: simulationId,
                    Status: SimulationAdvanceExecutionStatus.NotFound);

            bool advanced = false;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    clock.Advance(realDelta);

                    SimulationTimeAdvancedDomainEvent? advancedEvent = clock.DomainEvents
                       .OfType<SimulationTimeAdvancedDomainEvent>()
                       .LastOrDefault();

                    if (advancedEvent is not null)
                    {
                        advanced = true;

                        CityWeather? cityWeather = await weatherAdvanceExecutor.AdvanceAsync(
                            cityId: cityId,
                            evaluatedAt: advancedEvent.To,
                            cancellationToken: ct);

                        await outboxWriter.AddCityTimeAdvancedAsync(
                            cityId: advancedEvent.CityId,
                            from: advancedEvent.From,
                            to: advancedEvent.To,
                            tickId: advancedEvent.TickId,
                            speed: advancedEvent.Speed,
                            cancellationToken: ct);

                        if (cityWeather is not null && cityWeather.DomainEvents.Count > 0)
                        {
                            await outboxWriter.AddWeatherEventsAsync(
                                domainEvents: cityWeather.DomainEvents,
                                cancellationToken: ct);
                            cityWeather.ClearDomainEvents();
                        }
                    }

                    clock.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new SimulationAdvanceExecutionResult(
                SimulationId: simulationId,
                Status: advanced
                    ? SimulationAdvanceExecutionStatus.Advanced
                    : SimulationAdvanceExecutionStatus.Skipped);
        }
    }
}
