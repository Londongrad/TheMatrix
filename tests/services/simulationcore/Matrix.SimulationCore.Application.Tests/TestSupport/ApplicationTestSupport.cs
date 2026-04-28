using Matrix.BuildingBlocks.Application.Abstractions;
using System.Data;

namespace Matrix.SimulationCore.Application.Tests.TestSupport;

internal static class ApplicationTestSupport
{
    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotSupportedException();

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) => throw new NotSupportedException();
    }

    internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }

        public void SetUtcNow(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }
    }
}
