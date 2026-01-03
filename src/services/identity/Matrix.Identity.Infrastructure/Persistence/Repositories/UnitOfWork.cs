using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UnitOfWork(IdentityDbContext dbContext) : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool saveChanges = true)
        {
            return ExecuteInTransactionAsync<object?>(
                action: async ct =>
                {
                    await action(ct);
                    return null;
                },
                cancellationToken: cancellationToken,
                isolationLevel: isolationLevel,
                saveChanges: saveChanges);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            bool saveChanges = true)
        {
            // Ensures correct behavior with transient failures (SQL Server retries, etc.)
            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                // If an outer transaction already exists, do not create a nested transaction.
                if (dbContext.Database.CurrentTransaction is not null)
                {
                    T result = await action(cancellationToken);

                    if (saveChanges)
                        await dbContext.SaveChangesAsync(cancellationToken);

                    return result;
                }

                await using IDbContextTransaction tx =
                    await dbContext.Database.BeginTransactionAsync(
                        isolationLevel: isolationLevel,
                        cancellationToken: cancellationToken);

                try
                {
                    T result = await action(cancellationToken);

                    if (saveChanges)
                        await dbContext.SaveChangesAsync(cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
    }
}
