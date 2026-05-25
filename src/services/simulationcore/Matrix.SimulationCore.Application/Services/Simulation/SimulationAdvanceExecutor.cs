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
        ISimulationFixedStepSettings fixedStepSettings,
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

            int stepsProcessed = 0;
            long remainingPendingSimulationTicks = 0;
            bool hasRemainingBacklog = false;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    clock.AccumulatePendingSimulationTime(realDelta);

                    var fixedStep = TimeSpan.FromSeconds(fixedStepSettings.FixedStepSeconds);
                    ISimulationScenarioAdvanceHandler? handler = scenarioAdvanceHandlers
                       .FirstOrDefault(x => x.HostKind == host.HostKind);

                    while (stepsProcessed < fixedStepSettings.MaxStepsPerSimulationPerCycle &&
                           clock.TryAdvanceFixedStep(fixedStep))
                    {
                        SimulationTimeAdvancedDomainEvent advancedEvent = clock.DomainEvents
                           .OfType<SimulationTimeAdvancedDomainEvent>()
                           .Last();

                        if (handler is not null)
                            await handler.HandleAdvancedAsync(
                                host: host,
                                advancedEvent: advancedEvent,
                                cancellationToken: ct);

                        stepsProcessed++;
                        clock.ClearDomainEvents();
                    }

                    remainingPendingSimulationTicks = clock.PendingSimulationTicks;
                    hasRemainingBacklog = stepsProcessed == fixedStepSettings.MaxStepsPerSimulationPerCycle &&
                                          remainingPendingSimulationTicks >= fixedStep.Ticks;

                    clock.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new SimulationAdvanceExecutionResult(
                SimulationId: simulationId,
                Status: stepsProcessed > 0
                    ? SimulationAdvanceExecutionStatus.Advanced
                    : SimulationAdvanceExecutionStatus.NoStepDue,
                StepsProcessed: stepsProcessed,
                RemainingPendingSimulationTicks: remainingPendingSimulationTicks,
                HasRemainingBacklog: hasRemainingBacklog);
        }
    }
}
