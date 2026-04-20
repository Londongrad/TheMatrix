using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Matrix.BuildingBlocks.Infrastructure.Persistence
{
    public static class EfCoreTransactionExecutor
    {
        public static async Task<T> ExecuteAsync<TDbContext, T>(
            TDbContext dbContext,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            where TDbContext : DbContext
        {
            if (dbContext.Database.CurrentTransaction is not null)
                return await action(cancellationToken);

            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(
                    isolationLevel: isolationLevel,
                    cancellationToken: cancellationToken);

                T result = await action(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return result;
            });
        }
    }
}
