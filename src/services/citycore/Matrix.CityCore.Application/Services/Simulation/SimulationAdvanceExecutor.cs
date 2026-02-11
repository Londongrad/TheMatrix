using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Events.Simulation;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed class SimulationAdvanceExecutor(
        ISimulationClockRepository repository,
        ISimulationHostReadRepository simulationHostRepository,
        IReadOnlyCollection<ISimulationScenarioAdvanceHandler> scenarioAdvanceHandlers,
        IUnitOfWork unitOfWork) : ISimulationAdvanceExecutor
    {
        public async Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            SimulationHost? host = await simulationHostRepository.GetBySimulationIdAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            if (host is null)
                return new SimulationAdvanceExecutionResult(
                    SimulationId: simulationId,
                    Status: SimulationAdvanceExecutionStatus.NotFound);

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

                        ISimulationScenarioAdvanceHandler? handler = scenarioAdvanceHandlers
                           .FirstOrDefault(x => x.HostKind == host.HostKind);

                        if (handler is not null)
                            await handler.HandleAdvancedAsync(
                                host: host,
                                advancedEvent: advancedEvent,
                                cancellationToken: ct);
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
