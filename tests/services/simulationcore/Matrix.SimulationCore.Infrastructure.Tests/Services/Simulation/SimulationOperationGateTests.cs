using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Services.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation
{
    public sealed class SimulationOperationGateTests
    {
        [Fact]
        public async Task ExecuteAsync_WithSameSimulationId_SerializesActions()
        {
            var gate = new SimulationOperationGate();
            SimulationId simulationId = new(Guid.NewGuid());
            var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            bool secondStarted = false;

            Task first = gate.ExecuteAsync(
                simulationId: simulationId,
                action: async ct =>
                {
                    firstEntered.SetResult();
                    await releaseFirst.Task.WaitAsync(ct);
                },
                cancellationToken: CancellationToken.None);

            await firstEntered.Task;

            Task second = gate.ExecuteAsync(
                simulationId: simulationId,
                action: ct =>
                {
                    secondStarted = true;
                    return Task.CompletedTask;
                },
                cancellationToken: CancellationToken.None);

            await Task.Delay(100);

            Assert.False(secondStarted);

            releaseFirst.SetResult();

            await Task.WhenAll(
                first,
                second);

            Assert.True(secondStarted);
        }

        [Fact]
        public async Task ExecuteAsync_WithDifferentSimulationIds_AllowsIndependentProgress()
        {
            var gate = new SimulationOperationGate();
            SimulationId firstSimulationId = new(Guid.NewGuid());
            SimulationId secondSimulationId = new(Guid.NewGuid());
            var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Task first = gate.ExecuteAsync(
                simulationId: firstSimulationId,
                action: async ct =>
                {
                    firstEntered.SetResult();
                    await releaseFirst.Task.WaitAsync(ct);
                },
                cancellationToken: CancellationToken.None);

            await firstEntered.Task;

            Task second = gate.ExecuteAsync(
                simulationId: secondSimulationId,
                action: ct =>
                {
                    secondEntered.SetResult();
                    return Task.CompletedTask;
                },
                cancellationToken: CancellationToken.None);

            await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.False(first.IsCompleted);
            Assert.True(second.IsCompleted);

            releaseFirst.SetResult();

            await Task.WhenAll(
                first,
                second);
        }
    }
}
