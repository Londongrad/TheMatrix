using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Matrix.BuildingBlocks.Infrastructure.Persistence
{
    public class EfCoreUnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork
        where TDbContext : DbContext
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            return ExecuteInTransactionAsync<object?>(
                action: async token =>
                {
                    await action(token);
                    return null;
                },
                cancellationToken: cancellationToken,
                isolationLevel: isolationLevel);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (dbContext.Database.CurrentTransaction is not null)
                return await action(cancellationToken);

            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(
                isolationLevel: isolationLevel,
                cancellationToken: cancellationToken);

            T result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
    }
}
