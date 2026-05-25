using System.Text.Json;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.Identity.Contracts.Internal.Events;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Security.Processor;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Processor
{
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
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)),
                logger: new TestLogger<SecurityStateChangeProcessor>());

            await processor.ProcessAsync(CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            User updatedUser = await database.DbContext.Users.AsNoTracking()
               .SingleAsync();
            OutboxMessage outbox = await database.DbContext.OutboxMessages.SingleAsync();
            UserSecurityStateChangedV1? payload =
                JsonSerializer.Deserialize<UserSecurityStateChangedV1>(
                    json: outbox.PayloadJson,
                    options: Json);

            Assert.Equal(
                expected: 2,
                actual: updatedUser.PermissionsVersion);
            Assert.Equal(
                expected: InternalEventTypes.UserSecurityStateChangedV1,
                actual: outbox.Type);
            Assert.Equal(
                expected: LaterUtc,
                actual: outbox.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(
                expected: user.Id,
                actual: payload.UserId);
            Assert.Equal(
                expected: 2,
                actual: payload.PermissionsVersion);
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
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)),
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
                expectedSpan:
                [
                    InternalEventTypes.DefaultUserAccessPolicyChangedV1,
                    InternalEventTypes.UserSecurityStateChangedV1
                ],
                actualArray: outboxTypes);
            Assert.All(
                collection: occurredAtValues,
                action: occurredAtUtc => Assert.Equal(
                    expected: LaterUtc,
                    actual: occurredAtUtc));
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
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)),
                logger: logger);

            await processor.ProcessAsync(CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            OutboxMessage outbox = await database.DbContext.OutboxMessages.SingleAsync();
            DefaultUserAccessPolicyChangedV1? payload =
                JsonSerializer.Deserialize<DefaultUserAccessPolicyChangedV1>(
                    json: outbox.PayloadJson,
                    options: Json);

            Assert.Equal(
                expected: InternalEventTypes.DefaultUserAccessPolicyChangedV1,
                actual: outbox.Type);
            Assert.Equal(
                expected: LaterUtc,
                actual: outbox.OccurredOnUtc);
            Assert.NotNull(payload);
            Assert.Equal(
                expected: 7,
                actual: payload.Version);
            Assert.DoesNotContain(
                collection: logger.Entries,
                filter: entry => entry.LogLevel == LogLevel.Warning);
        }

        [Fact]
        public async Task ProcessAsync_WhenManyUsersChanged_BumpsVersionsAndWritesOutboxMessagesAcrossBatches()
        {
            const int userCount = 501;
            await using IdentityTestDatabase database = CreateDbContext();
            User[] users = Enumerable.Range(
                    start: 0,
                    count: userCount)
               .Select(index => CreateUser(
                    email: $"user-{index:0000}@matrix.local",
                    username: $"user{index:0000}"))
               .ToArray();
            await database.DbContext.Users.AddRangeAsync(users);
            await database.DbContext.SaveChangesAsync();

            var collector = new FakeSecurityStateChangeCollector();
            foreach (User user in users)
                collector.MarkUserChanged(user.Id);

            var processor = new SecurityStateChangeProcessor(
                dbContext: database.DbContext,
                defaultUserAccessPolicyRepository: new FakeDefaultUserAccessPolicyRepository(),
                collector: collector,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)),
                logger: new TestLogger<SecurityStateChangeProcessor>());

            await processor.ProcessAsync(CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            User[] updatedUsers = await database.DbContext.Users
               .AsNoTracking()
               .OrderBy(user => user.Email.Value)
               .ToArrayAsync();
            OutboxMessage[] outboxes = await database.DbContext.OutboxMessages
               .AsNoTracking()
               .ToArrayAsync();
            UserSecurityStateChangedV1[] payloads = outboxes
               .Select(outbox => JsonSerializer.Deserialize<UserSecurityStateChangedV1>(
                    json: outbox.PayloadJson,
                    options: Json))
               .OfType<UserSecurityStateChangedV1>()
               .ToArray();

            Assert.Equal(
                expected: userCount,
                actual: updatedUsers.Length);
            Assert.All(
                collection: updatedUsers,
                action: user => Assert.Equal(
                    expected: 2,
                    actual: user.PermissionsVersion));
            Assert.Equal(
                expected: userCount,
                actual: outboxes.Length);
            Assert.All(
                collection: outboxes,
                action: outbox =>
                {
                    Assert.Equal(
                        expected: InternalEventTypes.UserSecurityStateChangedV1,
                        actual: outbox.Type);
                    Assert.Equal(
                        expected: LaterUtc,
                        actual: outbox.OccurredOnUtc);
                });
            Assert.Equal(
                expected: userCount,
                actual: payloads.Length);
            Assert.All(
                collection: payloads,
                action: payload => Assert.Equal(
                    expected: 2,
                    actual: payload.PermissionsVersion));
            Assert.Equal(
                expected: users.Select(user => user.Id)
                   .OrderBy(id => id)
                   .ToArray(),
                actual: payloads.Select(payload => payload.UserId)
                   .OrderBy(id => id)
                   .ToArray());
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
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)),
                logger: logger);

            await processor.ProcessAsync(CancellationToken.None);
            await database.DbContext.SaveChangesAsync();

            Assert.Equal(
                expected: 1,
                actual: await database.DbContext.OutboxMessages.CountAsync());
            Assert.True(logger.Entries.Count(x => x.LogLevel == LogLevel.Warning) >= 2);
        }
    }
}
