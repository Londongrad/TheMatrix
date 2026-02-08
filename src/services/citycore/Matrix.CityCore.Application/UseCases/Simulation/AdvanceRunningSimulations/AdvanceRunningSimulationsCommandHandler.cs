using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceRunningSimulations
{
    public sealed class AdvanceRunningSimulationsCommandHandler(
        ISimulationClockRepository repository,
        ISimulationAdvanceExecutor executor,
        ILogger<AdvanceRunningSimulationsCommandHandler> logger)
        : IRequestHandler<AdvanceRunningSimulationsCommand, AdvanceRunningSimulationsResult>
    {
        public async Task<AdvanceRunningSimulationsResult> Handle(
            AdvanceRunningSimulationsCommand request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationId> simulationIds = await repository.ListActiveRunningSimulationIdsAsync(
                cancellationToken: cancellationToken);

            int advancedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            foreach (SimulationId simulationId in simulationIds)
                try
                {
                    SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                        simulationId: simulationId,
                        realDelta: request.RealDelta,
                        cancellationToken: cancellationToken);

                    switch (result.Status)
                    {
                        case SimulationAdvanceExecutionStatus.Advanced:
                            advancedCount++;
                            break;

                        case SimulationAdvanceExecutionStatus.Skipped:
                        case SimulationAdvanceExecutionStatus.NotFound:
                            skippedCount++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;

                    logger.LogError(
                        exception: ex,
                        message: "Failed to advance simulation {SimulationId}.",
                        args: simulationId.Value);
                }

            return new AdvanceRunningSimulationsResult(
                ProcessedCount: simulationIds.Count,
                AdvancedCount: advancedCount,
                SkippedCount: skippedCount,
                FailedCount: failedCount);
        }
    }
}
