using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Contracts.Internal.Events;
using Matrix.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Matrix.Identity.Infrastructure.Security.Processor
{
    /// <summary>
    ///     Applies security state changes to users and writes outbox messages.
    /// </summary>
    public sealed class SecurityStateChangeProcessor(
        IdentityDbContext dbContext,
        IDefaultUserAccessPolicyRepository defaultUserAccessPolicyRepository,
        ISecurityStateChangeCollector collector,
        TimeProvider timeProvider,
        ILogger<SecurityStateChangeProcessor> logger)
        : ISecurityStateChangeProcessor
    {
        private const int MaxMissingIdsToLog = 10;
        private const int UserSecurityStateBatchSize = 500;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Guid> changedUserIds = collector.DrainUsers();
            bool defaultUserAccessChanged = collector.DrainDefaultUserAccessChanged();

            if (changedUserIds.Count == 0 && !defaultUserAccessChanged)
                return;

            Guid[] userIds = changedUserIds.Distinct()
               .ToArray();
            int expected = userIds.Length;
            DateTime occurredOnUtc = _timeProvider.GetUtcNow()
               .UtcDateTime;

            if (userIds.Length > 0)
            {
                int affected = await BumpPermissionsVersionAsync(
                    userIds: userIds,
                    cancellationToken: cancellationToken);

                if (affected != expected)
                    logger.LogWarning(
                        message:
                        "PermissionsVersion bump mismatch (ExecuteUpdate). Expected to update {Expected}, but updated {Affected}.",
                        expected,
                        affected);

                List<UserPermissionsVersionProjection> versions =
                    await LoadUserPermissionVersionsAsync(
                        userIds: userIds,
                        cancellationToken: cancellationToken);

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

            if (!defaultUserAccessChanged)
                return;

            int defaultUserAccessVersion = await defaultUserAccessPolicyRepository.GetVersionAsync(cancellationToken);
            var defaultUserAccessPayload = new DefaultUserAccessPolicyChangedV1(defaultUserAccessVersion);

            dbContext.OutboxMessages.Add(
                OutboxMessage.Create(
                    type: InternalEventTypes.DefaultUserAccessPolicyChangedV1,
                    occurredOnUtc: occurredOnUtc,
                    payload: defaultUserAccessPayload,
                    jsonOptions: JsonOptions));
        }

        private async Task<int> BumpPermissionsVersionAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken)
        {
            int affected = 0;

            foreach (Guid[] batch in userIds.Chunk(UserSecurityStateBatchSize))
                affected += await dbContext.Users
                   .Where(u => batch.Contains(u.Id))
                   .ExecuteUpdateAsync(
                        setPropertyCalls: setters => setters.SetProperty(
                            u => u.PermissionsVersion,
                            u => u.PermissionsVersion + 1),
                        cancellationToken: cancellationToken);

            return affected;
        }

        private async Task<List<UserPermissionsVersionProjection>> LoadUserPermissionVersionsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken)
        {
            var versions = new List<UserPermissionsVersionProjection>(userIds.Count);

            foreach (Guid[] batch in userIds.Chunk(UserSecurityStateBatchSize))
            {
                List<UserPermissionsVersionProjection> batchVersions = await dbContext.Users
                   .AsNoTracking()
                   .Where(u => batch.Contains(u.Id))
                   .Select(u => new UserPermissionsVersionProjection(
                        u.Id,
                        u.PermissionsVersion))
                   .ToListAsync(cancellationToken);

                versions.AddRange(batchVersions);
            }

            return versions;
        }

        private readonly record struct UserPermissionsVersionProjection(
            Guid Id,
            int PermissionsVersion);
    }
}
