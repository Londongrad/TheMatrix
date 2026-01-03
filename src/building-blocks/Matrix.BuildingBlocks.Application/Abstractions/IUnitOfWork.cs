using System.Data;

namespace Matrix.BuildingBlocks.Application.Abstractions
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Executes the provided action inside a database transaction.
        /// If there is an active transaction, the action is executed within it (no nested transaction is created).
        /// By default, SaveChanges is called once and then the transaction is committed.
        /// </summary>
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool saveChanges = true);

        /// <summary>
        /// Executes the provided action inside a database transaction and returns a result.
        /// </summary>
        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool saveChanges = true);
    }
}
