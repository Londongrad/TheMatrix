using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Simulation
{
    public sealed class SimulationAdvanceExecutor(
        ISimulationClockRepository repository,
        ISimulationHostReadRepository simulationHostRepository,
        IEnumerable<ISimulationScenarioAdvanceHandler> scenarioAdvanceHandlers,
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
