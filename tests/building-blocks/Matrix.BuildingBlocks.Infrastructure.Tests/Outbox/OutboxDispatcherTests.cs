using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Dispatching;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Options;
using Matrix.BuildingBlocks.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.BuildingBlocks.Infrastructure.Tests.Outbox;

public sealed class OutboxDispatcherTests
{
    [Fact]
    public async Task DispatchOnceAsync_WhenBatchPublishes_MarksProcessedAndRunsCleanup()
    {
        DateTimeOffset now = new(2026, 5, 19, 4, 0, 0, TimeSpan.Zero);
        FakeOutboxRepository repository = new()
        {
            BatchToLease =
            [
                new LeasedOutboxMessage(Guid.NewGuid(), "population.sync", """{"id":1}""", 0),
                new LeasedOutboxMessage(Guid.NewGuid(), "economy.sync", """{"id":2}""", 1)
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

        Assert.Equal(2, publisher.Published.Count);
        Assert.Equal(2, repository.Processed.Count);
        Assert.Empty(repository.Failed);
        Assert.Equal(now.UtcDateTime, repository.LeaseNowUtc);
        Assert.Equal(now.AddSeconds(30).UtcDateTime, repository.LeaseLockedUntilUtc);
        Assert.Single(repository.CleanupRequests);
        Assert.Equal(now.AddSeconds(-3600).UtcDateTime, repository.CleanupRequests[0].ProcessedBeforeUtc);
        Assert.Equal(100, repository.CleanupRequests[0].BatchSize);
    }

    [Fact]
    public async Task DispatchOnceAsync_WhenTransientPublishFails_MarksFailedAndStopsRemainingBatch()
    {
        LeasedOutboxMessage first = new(Guid.NewGuid(), "population.sync", """{"id":1}""", 0);
        LeasedOutboxMessage second = new(Guid.NewGuid(), "economy.sync", """{"id":2}""", 0);
        DateTimeOffset now = new(2026, 5, 19, 4, 0, 0, TimeSpan.Zero);
        FakeOutboxRepository repository = new()
        {
            BatchToLease = [first, second]
        };
        FakeOutboxPublisher publisher = new()
        {
            PublishFailureFactory = (messageId, _, _) => messageId == first.Id ? new TimeoutException("temporary") : null
        };
        OutboxDispatcher dispatcher = CreateDispatcher(
            repository: repository,
            publisher: publisher,
            timeProvider: new FixedTimeProvider(now),
            options: new OutboxOptions());

        await dispatcher.DispatchOnceAsync(CancellationToken.None);

        Assert.Empty(repository.Processed);
        Assert.Single(repository.Failed);
        Assert.Equal(first.Id, repository.Failed[0].MessageId);
        Assert.Equal(now.AddSeconds(2).UtcDateTime, repository.Failed[0].NextAttemptOnUtc);
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task DispatchOnceAsync_WhenNonTransientPublishFails_ContinuesWithRemainingMessages()
    {
        LeasedOutboxMessage first = new(Guid.NewGuid(), "population.sync", """{"id":1}""", 2);
        LeasedOutboxMessage second = new(Guid.NewGuid(), "economy.sync", """{"id":2}""", 0);
        DateTimeOffset now = new(2026, 5, 19, 4, 0, 0, TimeSpan.Zero);
        FakeOutboxRepository repository = new()
        {
            BatchToLease = [first, second]
        };
        FakeOutboxPublisher publisher = new()
        {
            PublishFailureFactory = (messageId, _, _) => messageId == first.Id ? new InvalidOperationException("bad payload") : null
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
        Assert.Equal(now.AddSeconds(4).UtcDateTime, repository.Failed[0].NextAttemptOnUtc);
        Assert.Single(repository.Processed);
        Assert.Equal(second.Id, repository.Processed[0].MessageId);
        Assert.Single(publisher.Published);
        Assert.Equal(second.Id, publisher.Published[0].MessageId);
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
