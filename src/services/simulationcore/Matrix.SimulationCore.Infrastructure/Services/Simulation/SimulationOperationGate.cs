using System.Collections.Concurrent;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Infrastructure.Services.Simulation
{
    public sealed class SimulationOperationGate
    {
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

        public async Task<T> ExecuteAsync<T>(
            SimulationId simulationId,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken)
        {
            SemaphoreSlim gate = _locks.GetOrAdd(
                key: simulationId.Value,
                valueFactory: _ => new SemaphoreSlim(
                    initialCount: 1,
                    maxCount: 1));

            await gate.WaitAsync(cancellationToken);

            try
            {
                return await action(cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }

        public Task ExecuteAsync(
            SimulationId simulationId,
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                simulationId: simulationId,
                action: async ct =>
                {
                    await action(ct);
                    return true;
                },
                cancellationToken: cancellationToken);
        }
    }
}
