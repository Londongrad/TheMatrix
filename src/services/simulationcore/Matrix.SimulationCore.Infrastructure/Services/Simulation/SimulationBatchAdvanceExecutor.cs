using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Matrix.SimulationCore.Infrastructure.Services.Simulation
{
    public sealed class SimulationBatchAdvanceExecutor(
        IServiceScopeFactory scopeFactory,
        SimulationOperationGate operationGate,
        ILogger<SimulationBatchAdvanceExecutor> logger) : ISimulationBatchAdvanceExecutor
    {
        private const int MaxAttempts = 3;

        public async Task<SimulationBatchAdvanceResult> ExecuteAsync(
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationId> simulationIds = await ListSimulationIdsAsync(cancellationToken);

            int advancedCount = 0;
            int noStepDueCount = 0;
            int laggingCount = 0;
            int failedCount = 0;
            int totalStepsProcessed = 0;

            foreach (SimulationId simulationId in simulationIds)
            {
                SimulationAdvanceOutcome outcome = await AdvanceSimulationAsync(
                    simulationId: simulationId,
                    realDelta: realDelta,
                    cancellationToken: cancellationToken);

                switch (outcome)
                {
                    case
                    {
                        Failed: true
                    }:
                        failedCount++;
                        break;

                    case
                    {
                        Status: SimulationAdvanceExecutionStatus.Advanced
                    }:
                        advancedCount++;
                        break;

                    case
                    {
                        Status: SimulationAdvanceExecutionStatus.NoStepDue
                    }:
                        noStepDueCount++;
                        break;

                    case
                    {
                        Status: SimulationAdvanceExecutionStatus.NotFound
                    }:
                        failedCount++;
                        break;
                }

                if (outcome.HasRemainingBacklog)
                    laggingCount++;

                totalStepsProcessed += outcome.StepsProcessed;
            }

            return new SimulationBatchAdvanceResult(
                ProcessedCount: simulationIds.Count,
                AdvancedCount: advancedCount,
                NoStepDueCount: noStepDueCount,
                LaggingCount: laggingCount,
                FailedCount: failedCount,
                TotalStepsProcessed: totalStepsProcessed);
        }

        private async Task<IReadOnlyList<SimulationId>> ListSimulationIdsAsync(CancellationToken cancellationToken)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

            ISimulationClockRepository repository =
                scope.ServiceProvider.GetRequiredService<ISimulationClockRepository>();

            return await repository.ListActiveRunningSimulationIdsAsync(cancellationToken);
        }

        private async Task<SimulationAdvanceOutcome> AdvanceSimulationAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            return await operationGate.ExecuteAsync(
                simulationId: simulationId,
                action: async ct =>
                {
                    for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

                            ISimulationAdvanceExecutor executor =
                                scope.ServiceProvider.GetRequiredService<ISimulationAdvanceExecutor>();

                            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                                simulationId: simulationId,
                                realDelta: realDelta,
                                cancellationToken: ct);

                            return SimulationAdvanceOutcome.FromResult(result);
                        }
                        catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
                        {
                            logger.LogWarning(
                                exception: ex,
                                message:
                                "Concurrent update detected while advancing simulation {SimulationId}. Retrying attempt {Attempt} of {MaxAttempts}.",
                                args:
                                [
                                    simulationId.Value,
                                    attempt + 1,
                                    MaxAttempts
                                ]);
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            logger.LogWarning(
                                exception: ex,
                                message:
                                "Simulation {SimulationId} kept changing during background tick after {MaxAttempts} attempts. Skipping until the next tick.",
                                args:
                                [
                                    simulationId.Value,
                                    MaxAttempts
                                ]);

                            return SimulationAdvanceOutcome.FailedResult;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                exception: ex,
                                message: "Failed to advance simulation {SimulationId}.",
                                args: simulationId.Value);

                            return SimulationAdvanceOutcome.FailedResult;
                        }
                    }

                    return SimulationAdvanceOutcome.FailedResult;
                },
                cancellationToken: cancellationToken);
        }

        private sealed record SimulationAdvanceOutcome(
            SimulationAdvanceExecutionStatus Status,
            int StepsProcessed,
            bool HasRemainingBacklog,
            bool Failed = false)
        {
            public static SimulationAdvanceOutcome FailedResult { get; } = new(
                Status: SimulationAdvanceExecutionStatus.NotFound,
                StepsProcessed: 0,
                HasRemainingBacklog: false,
                Failed: true);

            public static SimulationAdvanceOutcome FromResult(SimulationAdvanceExecutionResult result)
            {
                return new SimulationAdvanceOutcome(
                    Status: result.Status,
                    StepsProcessed: result.StepsProcessed,
                    HasRemainingBacklog: result.HasRemainingBacklog);
            }
        }
    }
}
