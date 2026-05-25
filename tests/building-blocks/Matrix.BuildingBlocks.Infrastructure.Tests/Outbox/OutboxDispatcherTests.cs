using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Dispatching;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Options;
using Matrix.BuildingBlocks.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.BuildingBlocks.Infrastructure.Tests.Outbox
{
    public sealed class OutboxDispatcherTests
    {
        [Fact]
        public async Task DispatchOnceAsync_WhenBatchPublishes_MarksProcessedAndRunsCleanup()
        {
            DateTimeOffset now = new(
                year: 2026,
                month: 5,
                day: 19,
                hour: 4,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            FakeOutboxRepository repository = new()
            {
                BatchToLease =
                [
                    new LeasedOutboxMessage(
                        Id: Guid.NewGuid(),
                        Type: "population.sync",
                        PayloadJson: """{"id":1}""",
                        AttemptCount: 0),
                    new LeasedOutboxMessage(
                        Id: Guid.NewGuid(),
                        Type: "economy.sync",
                        PayloadJson: """{"id":2}""",
                        AttemptCount: 1)
                ]
            };
            FakeOutboxPublisher publisher = new();
            OutboxDispatcher dispatcher = CreateDispatcher(
                repository: repository,
                publisher: publisher,
                timeProvider: new FixedTimeProvider(now),
                options: new OutboxOptions
                {
                    BatchSize = 25,
                    LeaseTtlSeconds = 30,
                    CleanupBatchSize = 100,
                    ProcessedRetentionSeconds = 3600
                });

            await dispatcher.DispatchOnceAsync(CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: publisher.Published.Count);
            Assert.Equal(
                expected: 2,
                actual: repository.Processed.Count);
            Assert.Empty(repository.Failed);
            Assert.Equal(
                expected: now.UtcDateTime,
                actual: repository.LeaseNowUtc);
            Assert.Equal(
                expected: now.AddSeconds(30)
                   .UtcDateTime,
                actual: repository.LeaseLockedUntilUtc);
            Assert.Single(repository.CleanupRequests);
            Assert.Equal(
                expected: now.AddSeconds(-3600)
                   .UtcDateTime,
                actual: repository.CleanupRequests[0].ProcessedBeforeUtc);
            Assert.Equal(
                expected: 100,
                actual: repository.CleanupRequests[0].BatchSize);
        }

        [Fact]
        public async Task DispatchOnceAsync_WhenTransientPublishFails_MarksFailedAndStopsRemainingBatch()
        {
            LeasedOutboxMessage first = new(
                Id: Guid.NewGuid(),
                Type: "population.sync",
                PayloadJson: """{"id":1}""",
                AttemptCount: 0);
            LeasedOutboxMessage second = new(
                Id: Guid.NewGuid(),
                Type: "economy.sync",
                PayloadJson: """{"id":2}""",
                AttemptCount: 0);
            DateTimeOffset now = new(
                year: 2026,
                month: 5,
                day: 19,
                hour: 4,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            FakeOutboxRepository repository = new()
            {
                BatchToLease =
                [
                    first,
                    second
                ]
            };
            FakeOutboxPublisher publisher = new()
            {
                PublishFailureFactory = (
                    messageId,
                    _,
                    _) => messageId == first.Id
                    ? new TimeoutException("temporary")
                    : null
            };
            OutboxDispatcher dispatcher = CreateDispatcher(
                repository: repository,
                publisher: publisher,
                timeProvider: new FixedTimeProvider(now),
                options: new OutboxOptions());

            await dispatcher.DispatchOnceAsync(CancellationToken.None);

            Assert.Empty(repository.Processed);
            Assert.Single(repository.Failed);
            Assert.Equal(
                expected: first.Id,
                actual: repository.Failed[0].MessageId);
            Assert.Equal(
                expected: now.AddSeconds(2)
                   .UtcDateTime,
                actual: repository.Failed[0].NextAttemptOnUtc);
            Assert.Empty(publisher.Published);
        }

        [Fact]
        public async Task DispatchOnceAsync_WhenNonTransientPublishFails_ContinuesWithRemainingMessages()
        {
            LeasedOutboxMessage first = new(
                Id: Guid.NewGuid(),
                Type: "population.sync",
                PayloadJson: """{"id":1}""",
                AttemptCount: 2);
            LeasedOutboxMessage second = new(
                Id: Guid.NewGuid(),
                Type: "economy.sync",
                PayloadJson: """{"id":2}""",
                AttemptCount: 0);
            DateTimeOffset now = new(
                year: 2026,
                month: 5,
                day: 19,
                hour: 4,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            FakeOutboxRepository repository = new()
            {
                BatchToLease =
                [
                    first,
                    second
                ]
            };
            FakeOutboxPublisher publisher = new()
            {
                PublishFailureFactory = (
                    messageId,
                    _,
                    _) => messageId == first.Id
                    ? new InvalidOperationException("bad payload")
                    : null
            };
            OutboxDispatcher dispatcher = CreateDispatcher(
                repository: repository,
                publisher: publisher,
                timeProvider: new FixedTimeProvider(now),
                options: new OutboxOptions
                {
                    CleanupBatchSize = 0
                });

            await dispatcher.DispatchOnceAsync(CancellationToken.None);

            Assert.Single(repository.Failed);
            Assert.Equal(
                expected: now.AddSeconds(4)
                   .UtcDateTime,
                actual: repository.Failed[0].NextAttemptOnUtc);
            Assert.Single(repository.Processed);
            Assert.Equal(
                expected: second.Id,
                actual: repository.Processed[0].MessageId);
            Assert.Single(publisher.Published);
            Assert.Equal(
                expected: second.Id,
                actual: publisher.Published[0].MessageId);
            Assert.Empty(repository.CleanupRequests);
        }

        private static OutboxDispatcher CreateDispatcher(
            FakeOutboxRepository repository,
            FakeOutboxPublisher publisher,
            TimeProvider timeProvider,
            OutboxOptions options)
        {
            return new OutboxDispatcher(
                repo: repository,
                publisher: publisher,
                timeProvider: timeProvider,
                options: Options.Create(options),
                logger: NullLogger<OutboxDispatcher>.Instance);
        }
    }
}
