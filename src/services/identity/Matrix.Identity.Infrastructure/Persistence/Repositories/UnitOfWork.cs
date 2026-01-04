using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UnitOfWork(
        IdentityDbContext dbContext,
        ISecurityStateChangeProcessor securityStateChangeProcessor) : IUnitOfWork
    {
        private const string UnitOfWorkErrorCode = "Infrastructure.UnitOfWorkFailed";

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
                action: async ct =>
                {
                    await action(ct);
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
            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    // Nested transaction: do not create/commit/rollback here.
                    if (dbContext.Database.CurrentTransaction is not null)
                    {
                        T result = await action(cancellationToken);

                        await dbContext.SaveChangesAsync(cancellationToken);

                        return result;
                    }

                    await using IDbContextTransaction tx =
                        await dbContext.Database.BeginTransactionAsync(
                            isolationLevel: isolationLevel,
                            cancellationToken: cancellationToken);

                    T result2 = await action(cancellationToken);

                    await dbContext.SaveChangesAsync(cancellationToken);
                    await securityStateChangeProcessor.ProcessAsync(cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                    return result2;
                });
            }
            catch (OperationCanceledException)
            {
                // Cancellation is not an infrastructure failure.
                throw;
            }
            catch (Exception ex)
            {
                throw new MatrixInfrastructureException(
                    code: UnitOfWorkErrorCode,
                    message: "Unit of work execution failed.",
                    innerException: ex);
            }
        }
    }
}
