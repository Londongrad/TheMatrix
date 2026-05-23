using System.Text.Json;
using Matrix.Identity.Contracts.Internal.Events;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Security.Processor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Processor;

public sealed class SecurityStateChangeProcessorTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ProcessAsync_BumpsPermissionsVersionAndWritesUserOutboxMessage()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        User user = CreateUser();
        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        var collector = new FakeSecurityStateChangeCollector();
        collector.MarkUserChanged(user.Id);

        var processor = new SecurityStateChangeProcessor(
            dbContext: database.DbContext,
            defaultUserAccessPolicyRepository: new FakeDefaultUserAccessPolicyRepository(),
            collector: collector,
            timeProvider: CreateTimeProvider(new DateTimeOffset(LaterUtc, TimeSpan.Zero)),
            logger: new TestLogger<SecurityStateChangeProcessor>());

        await processor.ProcessAsync(CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        User updatedUser = await database.DbContext.Users.AsNoTracking().SingleAsync();
        var outbox = await database.DbContext.OutboxMessages.SingleAsync();
        var payload = JsonSerializer.Deserialize<UserSecurityStateChangedV1>(outbox.PayloadJson, Json);

        Assert.Equal(2, updatedUser.PermissionsVersion);
        Assert.Equal(InternalEventTypes.UserSecurityStateChangedV1, outbox.Type);
        Assert.Equal(LaterUtc, outbox.OccurredOnUtc);
        Assert.NotNull(payload);
        Assert.Equal(user.Id, payload.UserId);
        Assert.Equal(2, payload.PermissionsVersion);
    }

    [Fact]
    public async Task ProcessAsync_WhenDefaultAccessChanges_WritesAdditionalOutboxMessage()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        User user = CreateUser();
        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        var collector = new FakeSecurityStateChangeCollector();
        collector.MarkUserChanged(user.Id);
        collector.MarkDefaultUserAccessChanged();

        var processor = new SecurityStateChangeProcessor(
            dbContext: database.DbContext,
            defaultUserAccessPolicyRepository: new FakeDefaultUserAccessPolicyRepository
            {
                Version = 5
            },
            collector: collector,
            timeProvider: CreateTimeProvider(new DateTimeOffset(LaterUtc, TimeSpan.Zero)),
            logger: new TestLogger<SecurityStateChangeProcessor>());

        await processor.ProcessAsync(CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        string[] outboxTypes = database.DbContext.OutboxMessages
            .Select(x => x.Type)
            .OrderBy(x => x)
            .ToArray();
        DateTime[] occurredAtValues = database.DbContext.OutboxMessages
            .Select(x => x.OccurredOnUtc)
            .ToArray();

        Assert.Equal(
            [InternalEventTypes.DefaultUserAccessPolicyChangedV1, InternalEventTypes.UserSecurityStateChangedV1],
            outboxTypes);
        Assert.All(occurredAtValues, occurredAtUtc => Assert.Equal(LaterUtc, occurredAtUtc));
    }

    [Fact]
    public async Task ProcessAsync_WhenOnlyDefaultAccessChanges_WritesDefaultOutboxWithoutUserUpdates()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var collector = new FakeSecurityStateChangeCollector();
        collector.MarkDefaultUserAccessChanged();
        var logger = new TestLogger<SecurityStateChangeProcessor>();
        var processor = new SecurityStateChangeProcessor(
            dbContext: database.DbContext,
            defaultUserAccessPolicyRepository: new FakeDefaultUserAccessPolicyRepository
            {
                Version = 7
            },
            collector: collector,
            timeProvider: CreateTimeProvider(new DateTimeOffset(LaterUtc, TimeSpan.Zero)),
            logger: logger);

        await processor.ProcessAsync(CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        var outbox = await database.DbContext.OutboxMessages.SingleAsync();
        var payload = JsonSerializer.Deserialize<DefaultUserAccessPolicyChangedV1>(outbox.PayloadJson, Json);

        Assert.Equal(InternalEventTypes.DefaultUserAccessPolicyChangedV1, outbox.Type);
        Assert.Equal(LaterUtc, outbox.OccurredOnUtc);
        Assert.NotNull(payload);
        Assert.Equal(7, payload.Version);
        Assert.DoesNotContain(logger.Entries, entry => entry.LogLevel == LogLevel.Warning);
    }

    [Fact]
    public async Task ProcessAsync_WhenCollectorContainsMissingUser_LogsWarningsAndSkipsMissingEntry()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        User existingUser = CreateUser();
        await database.DbContext.Users.AddAsync(existingUser);
        await database.DbContext.SaveChangesAsync();

        var collector = new FakeSecurityStateChangeCollector();
        collector.MarkUserChanged(existingUser.Id);
        collector.MarkUserChanged(Guid.Parse("0d09bf9e-217c-47f5-ae04-4b8d5f7192d6"));
        var logger = new TestLogger<SecurityStateChangeProcessor>();
        var processor = new SecurityStateChangeProcessor(
            dbContext: database.DbContext,
            defaultUserAccessPolicyRepository: new FakeDefaultUserAccessPolicyRepository(),
            collector: collector,
            timeProvider: CreateTimeProvider(new DateTimeOffset(LaterUtc, TimeSpan.Zero)),
            logger: logger);

        await processor.ProcessAsync(CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        Assert.Equal(1, await database.DbContext.OutboxMessages.CountAsync());
        Assert.True(logger.Entries.Count(x => x.LogLevel == LogLevel.Warning) >= 2);
    }
}
