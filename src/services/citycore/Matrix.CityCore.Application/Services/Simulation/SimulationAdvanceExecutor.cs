using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Simulation;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed class SimulationAdvanceExecutor(
        ISimulationClockRepository repository,
        ICityCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : ISimulationAdvanceExecutor
    {
        public async Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            CityId cityId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (clock is null)
                return new SimulationAdvanceExecutionResult(
                    CityId: cityId,
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

                        await outboxWriter.AddCityTimeAdvancedAsync(
                            cityId: advancedEvent.CityId,
                            from: advancedEvent.From,
                            to: advancedEvent.To,
                            tickId: advancedEvent.TickId,
                            speed: advancedEvent.Speed,
                            cancellationToken: ct);
                    }

                    clock.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new SimulationAdvanceExecutionResult(
                CityId: cityId,
                Status: advanced
                    ? SimulationAdvanceExecutionStatus.Advanced
                    : SimulationAdvanceExecutionStatus.Skipped);
        }
    }
}
