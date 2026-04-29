using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;

internal static class SimulationInfrastructureTestSupport
{
    internal sealed class FakeSimulationClockRepository : ISimulationClockRepository
    {
        public IReadOnlyList<SimulationId> ActiveSimulationIds { get; set; } = Array.Empty<SimulationId>();
        public int ListActiveRunningSimulationIdsCallCount { get; private set; }

        public Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(CancellationToken cancellationToken)
        {
            ListActiveRunningSimulationIdsCallCount++;
            return Task.FromResult(ActiveSimulationIds);
        }

        public Task<SimulationClock?> GetBySimulationIdAsync(SimulationId simulationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(SimulationClock clock, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteBySimulationIdAsync(SimulationId simulationId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeSimulationAdvanceExecutor : ISimulationAdvanceExecutor
    {
        public Dictionary<Guid, Queue<object>> OutcomesBySimulationId { get; } = [];
        public List<(SimulationId SimulationId, TimeSpan RealDelta)> Requests { get; } = [];

        public Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            SimulationId simulationId,
            TimeSpan realDelta,
            CancellationToken cancellationToken)
        {
            Requests.Add((simulationId, realDelta));

            if (!OutcomesBySimulationId.TryGetValue(simulationId.Value, out Queue<object>? outcomes) || outcomes.Count == 0)
            {
                return Task.FromResult(
                    new SimulationAdvanceExecutionResult(
                        simulationId,
                        SimulationAdvanceExecutionStatus.Advanced));
            }

            object next = outcomes.Dequeue();
            if (next is Exception exception)
                throw exception;

            return Task.FromResult((SimulationAdvanceExecutionResult)next);
        }
    }

    internal sealed class TestServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestServiceScope(serviceProvider);
    }

    private sealed class TestServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider => serviceProvider;
        public void Dispose() { }
    }
}
