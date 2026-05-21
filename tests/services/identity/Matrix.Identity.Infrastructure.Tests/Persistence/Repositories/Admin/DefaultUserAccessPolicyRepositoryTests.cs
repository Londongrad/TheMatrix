using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class DefaultUserAccessPolicyRepositoryTests
{
    [Fact]
    public async Task GetForUpdateAsync_WhenPolicyIsMissing_CreatesSingletonWithClockTimestamp()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new DefaultUserAccessPolicyRepository(
            database.DbContext,
            CreateTimeProvider(new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero)));

        DefaultUserAccessPolicy policy = await repository.GetForUpdateAsync(CancellationToken.None);

        Assert.Equal(DefaultUserAccessPolicy.SingletonId, policy.Id);
        Assert.Equal(1, policy.Version);
        Assert.Equal(CreatedAtUtc, policy.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, policy.UpdatedAtUtc);
    }

    [Fact]
    public async Task ReplaceOverridesAsync_WhenDesiredStateChanges_AddsUpdatesAndRemovesEntries()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new DefaultUserAccessPolicyRepository(
            database.DbContext,
            CreateTimeProvider(new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero)));
        DefaultUserAccessPolicy policy = await repository.GetForUpdateAsync(CancellationToken.None);

        await database.DbContext.SaveChangesAsync();
        await database.DbContext.Permissions.AddRangeAsync(
            CreatePermission("identity.users.read"),
            CreatePermission("identity.users.write"),
            CreatePermission("identity.audit.read"));
        await database.DbContext.DefaultUserAccessOverrides.AddRangeAsync(
            new DefaultUserAccessOverride(policy.Id, "identity.users.read", PermissionEffect.Allow),
            new DefaultUserAccessOverride(policy.Id, "identity.users.write", PermissionEffect.Deny));
        await database.DbContext.SaveChangesAsync();

        bool changed = await repository.ReplaceOverridesAsync(
            new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
            {
                ["identity.users.read"] = PermissionEffect.Deny,
                ["identity.audit.read"] = PermissionEffect.Allow
            },
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        IReadOnlyDictionary<string, PermissionEffect> overrides = await repository.GetOverridesAsync(CancellationToken.None);

        Assert.True(changed);
        Assert.Equal(2, overrides.Count);
        Assert.Equal(PermissionEffect.Deny, overrides["identity.users.read"]);
        Assert.Equal(PermissionEffect.Allow, overrides["identity.audit.read"]);
        Assert.DoesNotContain("identity.users.write", overrides.Keys);
    }

    [Fact]
    public async Task ReplaceOverridesAsync_WhenDesiredStateMatchesExisting_ReturnsFalse()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new DefaultUserAccessPolicyRepository(
            database.DbContext,
            CreateTimeProvider(new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero)));
        DefaultUserAccessPolicy policy = await repository.GetForUpdateAsync(CancellationToken.None);

        await database.DbContext.SaveChangesAsync();
        await database.DbContext.Permissions.AddAsync(CreatePermission("identity.users.read"));
        await database.DbContext.DefaultUserAccessOverrides.AddAsync(
            new DefaultUserAccessOverride(policy.Id, "identity.users.read", PermissionEffect.Allow));
        await database.DbContext.SaveChangesAsync();

        bool changed = await repository.ReplaceOverridesAsync(
            new Dictionary<string, PermissionEffect>(StringComparer.Ordinal)
            {
                ["identity.users.read"] = PermissionEffect.Allow
            },
            CancellationToken.None);
        int version = await repository.GetVersionAsync(CancellationToken.None);

        Assert.False(changed);
        Assert.Equal(1, version);
    }
}
