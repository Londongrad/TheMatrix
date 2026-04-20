using System.Data;
using System.Diagnostics.CodeAnalysis;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
                    exception: ex,
                    message:
                    "Security audit table is missing. Pending audit entries were dropped so the main transaction could continue.");

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (TryTranslateKnownDbException(
                                                   exception: ex,
                                                   translated: out MatrixApplicationException? translated))
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
            bool hasAmbientTransaction = dbContext.Database.CurrentTransaction is not null;

            try
            {
                return await EfCoreTransactionExecutor.ExecuteAsync<IdentityDbContext, T>(
                    dbContext: dbContext,
                    action: async ct =>
                    {
                        T result = await action(ct);

                        await SaveChangesAsync(ct);

                        if (!hasAmbientTransaction)
                        {
                            await securityStateChangeProcessor.ProcessAsync(ct);
                            await SaveChangesAsync(ct);
                        }

                        return result;
                    },
                    cancellationToken: cancellationToken,
                    isolationLevel: isolationLevel);
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
            [NotNullWhen(true)]
            out MatrixApplicationException? translated)
        {
            if (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: RolesNormalizedNameConstraint
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

            foreach (EntityEntry<SecurityAuditEventRecord> entry in dbContext.ChangeTracker
                        .Entries<SecurityAuditEventRecord>())
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
                   postgresException.MessageText.Contains(
                       value: "SecurityAuditEvents",
                       comparisonType: StringComparison.Ordinal);
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
