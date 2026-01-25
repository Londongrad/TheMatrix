using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
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
            IReadOnlyList<CityId> cityIds = await repository.ListActiveRunningCityIdsAsync(
                cancellationToken: cancellationToken);

            int advancedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            foreach (CityId cityId in cityIds)
                try
                {
                    SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                        cityId: cityId,
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
                        message: "Failed to advance simulation for city {CityId}.",
                        args: cityId.Value);
                }

            return new AdvanceRunningSimulationsResult(
                ProcessedCount: cityIds.Count,
                AdvancedCount: advancedCount,
                SkippedCount: skippedCount,
                FailedCount: failedCount);
        }
    }
}
