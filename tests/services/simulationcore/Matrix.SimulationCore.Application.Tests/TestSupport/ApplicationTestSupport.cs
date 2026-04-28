using Matrix.BuildingBlocks.Application.Abstractions;
using System.Data;

namespace Matrix.SimulationCore.Application.Tests.TestSupport;

internal static class ApplicationTestSupport
{
    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }
        public int ExecuteInTransactionCallCount { get; private set; }
        public IsolationLevel? LastIsolationLevel { get; private set; }
        public Exception? ExceptionToThrowAfterAction { get; set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            ExecuteInTransactionCallCount++;
            LastIsolationLevel = isolationLevel;
            return ExecuteAsync(action, cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            ExecuteInTransactionCallCount++;
            LastIsolationLevel = isolationLevel;
            return ExecuteAsync(action, cancellationToken);
        }

        private async Task ExecuteAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            await action(cancellationToken);

            if (ExceptionToThrowAfterAction is not null)
                throw ExceptionToThrowAfterAction;
        }

        private async Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken)
        {
            T result = await action(cancellationToken);

            if (ExceptionToThrowAfterAction is not null)
                throw ExceptionToThrowAfterAction;

            return result;
        }
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
