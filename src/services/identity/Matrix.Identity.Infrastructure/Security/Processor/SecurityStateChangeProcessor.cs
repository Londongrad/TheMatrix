using System.Text.Json;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Contracts.Internal.Events;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Infrastructure.Security.Processor
{
    /// <summary>
    ///     Applies security state changes to users and writes outbox messages.
    /// </summary>
    public sealed class SecurityStateChangeProcessor(
        IdentityDbContext dbContext,
        ISecurityStateChangeCollector collector,
        IClock clock,
        ILogger<SecurityStateChangeProcessor> logger)
        : ISecurityStateChangeProcessor
    {
        private const int MaxMissingIdsToLog = 10;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Guid> changedUserIds = collector.DrainUsers();

            if (changedUserIds.Count == 0)
                return;

            Guid[] userIds = changedUserIds.ToArray();
            int expected = userIds.Length;

            // TODO: Consider using batches if the number of users is large. "Contains" is expensive.
            int affected = await dbContext.Users
               .Where(u => userIds.Contains(u.Id))
               .ExecuteUpdateAsync(
                    setPropertyCalls: setters => setters.SetProperty(
                        u => u.PermissionsVersion,
                        u => u.PermissionsVersion + 1),
                    cancellationToken: cancellationToken);

            if (affected != expected)
                logger.LogWarning(
                    message:
                    "PermissionsVersion bump mismatch (ExecuteUpdate). Expected to update {Expected}, but updated {Affected}.",
                    expected,
                    affected);

            var versions = await dbContext.Users
               .AsNoTracking()
               .Where(u => userIds.Contains(u.Id))
               .Select(u => new
                {
                    u.Id,
                    u.PermissionsVersion
                })
               .ToListAsync(cancellationToken);

            if (versions.Count != expected)
            {
                var found = versions.Select(x => x.Id)
                   .ToHashSet();

                Guid[] missing = userIds.Where(id => !found.Contains(id))
                   .Take(MaxMissingIdsToLog)
                   .ToArray();

                int missingCount = expected - versions.Count;

                logger.LogWarning(
                    message:
                    "PermissionsVersion bump mismatch (Select). Expected {Expected} users, but loaded {Loaded}. MissingCount={MissingCount}. MissingSample={MissingSample}.",
                    expected,
                    versions.Count,
                    missingCount,
                    missing);
            }

            DateTime occurredOnUtc = clock.UtcNow;

            foreach (var version in versions)
            {
                var payload = new UserSecurityStateChangedV1(
                    UserId: version.Id,
                    PermissionsVersion: version.PermissionsVersion);

                dbContext.OutboxMessages.Add(
                    OutboxMessage.Create(
                        type: InternalEventTypes.UserSecurityStateChangedV1,
                        occurredOnUtc: occurredOnUtc,
                        payload: payload,
                        jsonOptions: JsonOptions));
            }
        }
    }
}
