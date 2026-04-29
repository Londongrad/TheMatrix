using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;

public sealed class SimulationBatchAdvanceExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_AggregatesAdvancedSkippedAndFailedCounts()
    {
        SimulationId advancedId = new(Guid.NewGuid());
        SimulationId skippedId = new(Guid.NewGuid());
        SimulationId failedId = new(Guid.NewGuid());
        var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository
        {
            ActiveSimulationIds = [advancedId, skippedId, failedId]
        };
        var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
        executor.OutcomesBySimulationId[advancedId.Value] = new Queue<object>(
        [
            new SimulationAdvanceExecutionResult(advancedId, SimulationAdvanceExecutionStatus.Advanced)
        ]);
        executor.OutcomesBySimulationId[skippedId.Value] = new Queue<object>(
        [
            new SimulationAdvanceExecutionResult(skippedId, SimulationAdvanceExecutionStatus.NotFound)
        ]);
        executor.OutcomesBySimulationId[failedId.Value] = new Queue<object>(
        [
            new InvalidOperationException("boom")
        ]);
        var batchExecutor = CreateExecutor(repository, executor);
        TimeSpan realDelta = TimeSpan.FromSeconds(2);

        SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(realDelta, CancellationToken.None);

        Assert.Equal(1, repository.ListActiveRunningSimulationIdsCallCount);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(1, result.AdvancedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(
            [advancedId.Value, skippedId.Value, failedId.Value],
            executor.Requests.Select(static x => x.SimulationId.Value).ToArray());
        Assert.All(executor.Requests, x => Assert.Equal(realDelta, x.RealDelta));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoActiveSimulations_ReturnsZeroCounts()
    {
        var repository = new SimulationInfrastructureTestSupport.FakeSimulationClockRepository();
        var executor = new SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor();
        var batchExecutor = CreateExecutor(repository, executor);

        SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.AdvancedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(executor.Requests);
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
            new SimulationAdvanceExecutionResult(simulationId, SimulationAdvanceExecutionStatus.Advanced)
        ]);
        var batchExecutor = CreateExecutor(repository, executor);

        SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.AdvancedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, executor.Requests.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConcurrencyConflictPersistsAcrossAllRetries_SkipsSimulation()
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
        var batchExecutor = CreateExecutor(repository, executor);

        SimulationBatchAdvanceResult result = await batchExecutor.ExecuteAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.AdvancedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(3, executor.Requests.Count);
    }

    private static SimulationBatchAdvanceExecutor CreateExecutor(
        SimulationInfrastructureTestSupport.FakeSimulationClockRepository repository,
        SimulationInfrastructureTestSupport.FakeSimulationAdvanceExecutor executor)
    {
        var serviceProvider = new Dictionary<Type, object>
        {
            [typeof(Matrix.SimulationCore.Application.Abstractions.Persistence.ISimulationClockRepository)] = repository,
            [typeof(Matrix.SimulationCore.Application.Services.Simulation.Abstractions.ISimulationAdvanceExecutor)] = executor
        };

        return new SimulationBatchAdvanceExecutor(
            new SimulationInfrastructureTestSupport.TestServiceScopeFactory(new DictionaryServiceProvider(serviceProvider)),
            new SimulationOperationGate(),
            NullLogger<SimulationBatchAdvanceExecutor>.Instance);
    }

    private sealed class DictionaryServiceProvider(Dictionary<Type, object> services) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            services.TryGetValue(serviceType, out object? service);
            return service;
        }
    }
}
