using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class DefaultUserAccessPolicyRepositoryTests
    {
        [Fact]
        public async Task GetForUpdateAsync_WhenPolicyIsMissing_CreatesSingletonWithClockTimestamp()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new DefaultUserAccessPolicyRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));

            DefaultUserAccessPolicy policy = await repository.GetForUpdateAsync(CancellationToken.None);

            Assert.Equal(
                expected: DefaultUserAccessPolicy.SingletonId,
                actual: policy.Id);
            Assert.Equal(
                expected: 1,
                actual: policy.Version);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: policy.CreatedAtUtc);
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: policy.UpdatedAtUtc);
        }

        [Fact]
        public async Task ReplaceOverridesAsync_WhenDesiredStateChanges_AddsUpdatesAndRemovesEntries()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new DefaultUserAccessPolicyRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));
            DefaultUserAccessPolicy policy = await repository.GetForUpdateAsync(CancellationToken.None);

            await database.DbContext.SaveChangesAsync();
            await database.DbContext.Permissions.AddRangeAsync(
                CreatePermission(),
                CreatePermission("identity.users.write"),
                CreatePermission("identity.audit.read"));
            await database.DbContext.DefaultUserAccessOverrides.AddRangeAsync(
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: "identity.users.read",
                    effect: PermissionEffect.Allow),
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: "identity.users.write",
                    effect: PermissionEffect.Deny));
            await database.DbContext.SaveChangesAsync();

            bool changed = await repository.ReplaceOverridesAsync(
                overrides: new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                {
                    ["identity.users.read"] = PermissionEffect.Deny,
                    ["identity.audit.read"] = PermissionEffect.Allow
                },
                cancellationToken: CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            IReadOnlyDictionary<string, PermissionEffect> overrides =
                await repository.GetOverridesAsync(CancellationToken.None);

            Assert.True(changed);
            Assert.Equal(
                expected: 2,
                actual: overrides.Count);
            Assert.Equal(
                expected: PermissionEffect.Deny,
                actual: overrides["identity.users.read"]);
            Assert.Equal(
                expected: PermissionEffect.Allow,
                actual: overrides["identity.audit.read"]);
            Assert.DoesNotContain(
                expected: "identity.users.write",
                collection: overrides.Keys);
        }

        [Fact]
        public async Task ReplaceOverridesAsync_WhenDesiredStateMatchesExisting_ReturnsFalse()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new DefaultUserAccessPolicyRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));
            DefaultUserAccessPolicy policy = await repository.GetForUpdateAsync(CancellationToken.None);

            await database.DbContext.SaveChangesAsync();
            await database.DbContext.Permissions.AddAsync(CreatePermission());
            await database.DbContext.DefaultUserAccessOverrides.AddAsync(
                new DefaultUserAccessOverride(
                    policyId: policy.Id,
                    permissionKey: "identity.users.read",
                    effect: PermissionEffect.Allow));
            await database.DbContext.SaveChangesAsync();

            bool changed = await repository.ReplaceOverridesAsync(
                overrides: new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
                {
                    ["identity.users.read"] = PermissionEffect.Allow
                },
                cancellationToken: CancellationToken.None);
            int version = await repository.GetVersionAsync(CancellationToken.None);

            Assert.False(changed);
            Assert.Equal(
                expected: 1,
                actual: version);
        }
    }
}
