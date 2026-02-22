using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Infrastructure.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class UnitOfWork(
        IdentityDbContext dbContext,
        ISecurityStateChangeProcessor securityStateChangeProcessor,
        ILogger<UnitOfWork> logger) : IUnitOfWork
    {
        private const string UnitOfWorkErrorCode = "Infrastructure.UnitOfWorkFailed";
        private const string RolesNormalizedNameConstraint = "ux_roles_normalized_name";

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (TryRecoverFromMissingSecurityAuditTable(ex))
            {
                logger.LogWarning(
                    ex,
                    "Security audit table is missing. Pending audit entries were dropped so the main transaction could continue.");

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (TryTranslateKnownDbException(ex, out MatrixApplicationException? translated))
            {
                throw translated;
            }
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
            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    // Nested transaction: do not create/commit/rollback here.
                    if (dbContext.Database.CurrentTransaction is not null)
                    {
                        T result = await action(cancellationToken);

                        await SaveChangesAsync(cancellationToken);

                        return result;
                    }

                    await using IDbContextTransaction tx =
                        await dbContext.Database.BeginTransactionAsync(
                            isolationLevel: isolationLevel,
                            cancellationToken: cancellationToken);

                    T result2 = await action(cancellationToken);

                    await SaveChangesAsync(cancellationToken);
                    await securityStateChangeProcessor.ProcessAsync(cancellationToken);
                    await SaveChangesAsync(cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                    return result2;
                });
            }
            catch (OperationCanceledException)
            {
                // Cancellation is not an infrastructure failure.
                throw;
            }
            catch (MatrixApplicationException)
            {
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

        private bool TryTranslateKnownDbException(
            DbUpdateException exception,
            out MatrixApplicationException? translated)
        {
            if (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: RolesNormalizedNameConstraint
                })
            {
                translated = ApplicationErrorsFactory.RoleNameAlreadyInUse(
                    GetTrackedRoleName() ?? "specified role name");
                return true;
            }

            translated = null;
            return false;
        }

        private bool TryRecoverFromMissingSecurityAuditTable(DbUpdateException exception)
        {
            if (!IsMissingSecurityAuditTable(exception))
                return false;

            bool detachedAny = false;

            foreach (EntityEntry<SecurityAuditEventRecord> entry in dbContext.ChangeTracker.Entries<SecurityAuditEventRecord>())
            {
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                entry.State = EntityState.Detached;
                detachedAny = true;
            }

            return detachedAny;
        }

        private static bool IsMissingSecurityAuditTable(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException &&
                   postgresException.SqlState == PostgresErrorCodes.UndefinedTable &&
                   postgresException.MessageText.Contains("SecurityAuditEvents", StringComparison.Ordinal);
        }

        private string? GetTrackedRoleName()
        {
            return dbContext.ChangeTracker
               .Entries<Role>()
               .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
               .Select(entry => entry.Entity.Name)
               .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
        }
    }
}
