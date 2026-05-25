using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation
{
    public sealed class SimulationBatchAdvanceExecutorTests
    {
        [Fact]
        public async Task ExecuteAsync_AggregatesFixedStepOutcomeCounts()
        {
            SimulationId advancedId = new(Guid.NewGuid());
            SimulationId noStepDueId = new(Guid.NewGuid());
            SimulationId laggingId = new(Guid.NewGuid());
            SimulationId failedId = new(Guid.NewGuid());
            var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository
            {
                ActiveSimulationIds =
                [
                    advancedId,
                    noStepDueId,
                    laggingId,
                    failedId
                ]
            };
            var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
            executor.OutcomesBySimulationId[advancedId.Value] = new Queue<object>(
            [
                new SimulationAdvanceExecutionResult(
                    SimulationId: advancedId,
                    Status: SimulationAdvanceExecutionStatus.Advanced,
                    StepsProcessed: 2)
            ]);
            executor.OutcomesBySimulationId[noStepDueId.Value] = new Queue<object>(
            [
                new SimulationAdvanceExecutionResult(
                    SimulationId: noStepDueId,
                    Status: SimulationAdvanceExecutionStatus.NoStepDue,
                    RemainingPendingSimulationTicks: TimeSpan.FromSeconds(30)
                       .Ticks)
            ]);
            executor.OutcomesBySimulationId[laggingId.Value] = new Queue<object>(
            [
                new SimulationAdvanceExecutionResult(
                    SimulationId: laggingId,
                    Status: SimulationAdvanceExecutionStatus.Advanced,
                    StepsProcessed: 10,
                    RemainingPendingSimulationTicks: TimeSpan.FromMinutes(20)
                       .Ticks,
                    HasRemainingBacklog: true)
            ]);
            executor.OutcomesBySimulationId[failedId.Value] = new Queue<object>(
                [new InvalidOperationException("boom")]);
            SimulationBatchAdvanceExecutor batchExecutor = CreateExecutor(
                repository: repository,
                executor: executor);
            var realDelta = TimeSpan.FromSeconds(2);

            SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(
                realDelta: realDelta,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: repository.ListActiveRunningSimulationIdsCallCount);
            Assert.Equal(
                expected: 4,
                actual: result.ProcessedCount);
            Assert.Equal(
                expected: 2,
                actual: result.AdvancedCount);
            Assert.Equal(
                expected: 1,
                actual: result.NoStepDueCount);
            Assert.Equal(
                expected: 1,
                actual: result.LaggingCount);
            Assert.Equal(
                expected: 1,
                actual: result.FailedCount);
            Assert.Equal(
                expected: 12,
                actual: result.TotalStepsProcessed);
            Assert.Equal(
                expectedSpan:
                [
                    advancedId.Value,
                    noStepDueId.Value,
                    laggingId.Value,
                    failedId.Value
                ],
                actualArray: executor.Requests.Select(static x => x.SimulationId.Value)
                   .ToArray());
            Assert.All(
                collection: executor.Requests,
                action: x => Assert.Equal(
                    expected: realDelta,
                    actual: x.RealDelta));
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoActiveSimulations_ReturnsZeroCounts()
        {
            var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository();
            var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
            SimulationBatchAdvanceExecutor batchExecutor = CreateExecutor(
                repository: repository,
                executor: executor);

            SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 0,
                actual: result.ProcessedCount);
            Assert.Equal(
                expected: 0,
                actual: result.AdvancedCount);
            Assert.Equal(
                expected: 0,
                actual: result.NoStepDueCount);
            Assert.Equal(
                expected: 0,
                actual: result.LaggingCount);
            Assert.Equal(
                expected: 0,
                actual: result.FailedCount);
            Assert.Equal(
                expected: 0,
                actual: result.TotalStepsProcessed);
            Assert.Empty(executor.Requests);
        }

        [Fact]
        public async Task ExecuteAsync_WhenSimulationIsNotFound_CountsFailure()
        {
            SimulationId simulationId = new(Guid.NewGuid());
            var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository
            {
                ActiveSimulationIds = [simulationId]
            };
            var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
            executor.OutcomesBySimulationId[simulationId.Value] = new Queue<object>(
            [
                new SimulationAdvanceExecutionResult(
                    SimulationId: simulationId,
                    Status: SimulationAdvanceExecutionStatus.NotFound)
            ]);
            SimulationBatchAdvanceExecutor batchExecutor = CreateExecutor(
                repository: repository,
                executor: executor);

            SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: result.ProcessedCount);
            Assert.Equal(
                expected: 0,
                actual: result.AdvancedCount);
            Assert.Equal(
                expected: 0,
                actual: result.NoStepDueCount);
            Assert.Equal(
                expected: 0,
                actual: result.LaggingCount);
            Assert.Equal(
                expected: 1,
                actual: result.FailedCount);
            Assert.Equal(
                expected: 0,
                actual: result.TotalStepsProcessed);
        }

        [Fact]
        public async Task ExecuteAsync_WhenConcurrencyConflictResolvesOnRetry_AdvancesSimulation()
        {
            SimulationId simulationId = new(Guid.NewGuid());
            var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository
            {
                ActiveSimulationIds = [simulationId]
            };
            var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
            executor.OutcomesBySimulationId[simulationId.Value] = new Queue<object>(
            [
                new DbUpdateConcurrencyException("conflict"),
                new SimulationAdvanceExecutionResult(
                    SimulationId: simulationId,
                    Status: SimulationAdvanceExecutionStatus.Advanced,
                    StepsProcessed: 1)
            ]);
            SimulationBatchAdvanceExecutor batchExecutor = CreateExecutor(
                repository: repository,
                executor: executor);

            SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: result.ProcessedCount);
            Assert.Equal(
                expected: 1,
                actual: result.AdvancedCount);
            Assert.Equal(
                expected: 0,
                actual: result.NoStepDueCount);
            Assert.Equal(
                expected: 0,
                actual: result.LaggingCount);
            Assert.Equal(
                expected: 0,
                actual: result.FailedCount);
            Assert.Equal(
                expected: 1,
                actual: result.TotalStepsProcessed);
            Assert.Equal(
                expected: 2,
                actual: executor.Requests.Count);
        }

        [Fact]
        public async Task ExecuteAsync_WhenConcurrencyConflictPersistsAcrossAllRetries_CountsFailure()
        {
            SimulationId simulationId = new(Guid.NewGuid());
            var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository
            {
                ActiveSimulationIds = [simulationId]
            };
            var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
            executor.OutcomesBySimulationId[simulationId.Value] = new Queue<object>(
            [
                new DbUpdateConcurrencyException("conflict-1"),
                new DbUpdateConcurrencyException("conflict-2"),
                new DbUpdateConcurrencyException("conflict-3")
            ]);
            SimulationBatchAdvanceExecutor batchExecutor = CreateExecutor(
                repository: repository,
                executor: executor);

            SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(
                realDelta: TimeSpan.FromSeconds(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: result.ProcessedCount);
            Assert.Equal(
                expected: 0,
                actual: result.AdvancedCount);
            Assert.Equal(
                expected: 0,
                actual: result.NoStepDueCount);
            Assert.Equal(
                expected: 0,
                actual: result.LaggingCount);
            Assert.Equal(
                expected: 1,
                actual: result.FailedCount);
            Assert.Equal(
                expected: 0,
                actual: result.TotalStepsProcessed);
            Assert.Equal(
                expected: 3,
                actual: executor.Requests.Count);
        }

        private static SimulationBatchAdvanceExecutor CreateExecutor(
            SimulationInfrastructureTestSupport.FakeSimulationClockRepository repository,
            SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor executor)
        {
            var serviceProvider = new Dictionary<Type, object>
            {
                [typeof(ISimulationClockRepository)] = repository,
                [typeof(ISimulationAdvanceExecutor)] = executor
            };

            return new SimulationBatchAdvanceExecutor(
                scopeFactory: new SimulationInfrastructureTestSupport.TestServiceScopeFactory(
                    new DictionaryServiceProvider(serviceProvider)),
                operationGate: new SimulationOperationGate(),
                logger: NullLogger<SimulationBatchAdvanceExecutor>.Instance);
        }

        private sealed class DictionaryServiceProvider(Dictionary<Type, object> services) : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                services.TryGetValue(
                    key: serviceType,
                    value: out object? service);
                return service;
            }
        }
    }
}
