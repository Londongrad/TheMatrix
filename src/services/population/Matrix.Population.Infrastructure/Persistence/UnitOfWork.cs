using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Matrix.Population.Infrastructure.Persistence
{
    public sealed class UnitOfWork(PopulationDbContext dbContext) : IUnitOfWork
    {
        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (dbContext.Database.CurrentTransaction is not null)
            {
                await action(cancellationToken);
                return;
            }

            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (dbContext.Database.CurrentTransaction is not null)
                return await action(cancellationToken);

            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            T result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
    }
}