using System.Diagnostics;
using Matrix.Population.Infrastructure.Messaging.Cleanup;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.TestSupport.PopulationInfrastructureTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Messaging.Cleanup
{
    public sealed class ProcessedIntegrationMessageCleanupHostedServiceTests
    {
        [Fact]
        public async Task StartAsync_WhenCleanupIsDisabled_LogsInformationAndStops()
        {
            var logger = new TestLogger<ProcessedIntegrationMessageCleanupHostedService>();
            ProcessedIntegrationMessageCleanupHostedService service = CreateService(
                scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                options: new ProcessedIntegrationMessageCleanupOptions
                {
                    CleanupEnabled = false
                },
                logger: logger);

            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);

            TestLogEntry entry = Assert.Single(
                collection: logger.Entries,
                predicate: x => x.LogLevel == LogLevel.Information);
            Assert.Contains(
                expectedSubstring: "cleanup is disabled",
                actualString: entry.Message);
        }

        [Fact]
        public async Task StartAsync_WhenPollIntervalIsNonPositive_LogsErrorAndStops()
        {
            var logger = new TestLogger<ProcessedIntegrationMessageCleanupHostedService>();
            ProcessedIntegrationMessageCleanupHostedService service = CreateService(
                scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                options: new ProcessedIntegrationMessageCleanupOptions
                {
                    CleanupEnabled = true,
                    PollIntervalSeconds = 0,
                    BatchSize = 1
                },
                logger: logger);

            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);

            TestLogEntry entry = Assert.Single(
                collection: logger.Entries,
                predicate: x => x.LogLevel == LogLevel.Error);
            Assert.Contains(
                expectedSubstring: "poll interval must be > 0",
                actualString: entry.Message);
        }

        [Fact]
        public async Task StartAsync_WhenBatchSizeIsNonPositive_LogsErrorAndStops()
        {
            var logger = new TestLogger<ProcessedIntegrationMessageCleanupHostedService>();
            ProcessedIntegrationMessageCleanupHostedService service = CreateService(
                scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                options: new ProcessedIntegrationMessageCleanupOptions
                {
                    CleanupEnabled = true,
                    PollIntervalSeconds = 1,
                    BatchSize = 0
                },
                logger: logger);

            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);

            TestLogEntry entry = Assert.Single(
                collection: logger.Entries,
                predicate: x => x.LogLevel == LogLevel.Error);
            Assert.Contains(
                expectedSubstring: "batch size must be > 0",
                actualString: entry.Message);
        }

        [Fact]
        public async Task StartAsync_WhenTickOccurs_DeletesExpiredMarkersAndLogsDebug()
        {
            await using PopulationTestDatabase database = CreateDbContext();
            PopulationDbContext dbContext = database.DbContext;
            dbContext.ProcessedIntegrationMessages.AddRange(
                new ProcessedIntegrationMessage(
                    consumer: "Old",
                    messageId: Guid.Parse("79cac35e-c707-4275-b355-0f4e61cffdda"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 1,
                        hour: 20,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                new ProcessedIntegrationMessage(
                    consumer: "Fresh",
                    messageId: Guid.Parse("6d3794d1-c3c1-43cd-96b4-65f5c7453479"),
                    processedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 2,
                        hour: 23,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)));
            await dbContext.SaveChangesAsync();
            var cleaner = new ProcessedIntegrationMessageCleaner(dbContext);
            var logger = new TestLogger<ProcessedIntegrationMessageCleanupHostedService>();
            ProcessedIntegrationMessageCleanupHostedService service = CreateService(
                scopeFactory: new TestServiceScopeFactory(
                    new DictionaryServiceProvider(
                        new Dictionary<Type, object>
                        {
                            [typeof(ProcessedIntegrationMessageCleaner)] = cleaner
                        })),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                options: new ProcessedIntegrationMessageCleanupOptions
                {
                    CleanupEnabled = true,
                    PollIntervalSeconds = 1,
                    BatchSize = 10,
                    RetentionHours = 24
                },
                logger: logger);

            await service.StartAsync(CancellationToken.None);
            await WaitUntilAsync(
                condition: () => logger.Entries.Any(x => x.LogLevel == LogLevel.Debug),
                timeout: TimeSpan.FromSeconds(3));
            await service.StopAsync(CancellationToken.None);

            ProcessedIntegrationMessage remaining = await dbContext.ProcessedIntegrationMessages.SingleAsync();
            TestLogEntry entry = Assert.Single(
                collection: logger.Entries,
                predicate: x => x.LogLevel == LogLevel.Debug);
            Assert.Equal(
                expected: "Fresh",
                actual: remaining.Consumer);
            Assert.Contains(
                expectedSubstring: "Deleted 1 processed integration message markers",
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: "05/02/2048 00:00:00 +00:00",
                actualString: entry.Message);
        }

        [Fact]
        public async Task StartAsync_WhenCleanupLoopThrows_LogsError()
        {
            var logger = new TestLogger<ProcessedIntegrationMessageCleanupHostedService>();
            ProcessedIntegrationMessageCleanupHostedService service = CreateService(
                scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                options: new ProcessedIntegrationMessageCleanupOptions
                {
                    CleanupEnabled = true,
                    PollIntervalSeconds = 1,
                    BatchSize = 1
                },
                logger: logger);

            await service.StartAsync(CancellationToken.None);
            await WaitUntilAsync(
                condition: () => logger.Entries.Any(x => x.LogLevel == LogLevel.Error && x.Exception is not null),
                timeout: TimeSpan.FromSeconds(3));
            await service.StopAsync(CancellationToken.None);

            TestLogEntry entry = Assert.Single(
                collection: logger.Entries,
                predicate: x => x.LogLevel == LogLevel.Error && x.Exception is not null);
            Assert.Contains(
                expectedSubstring: "cleanup loop failed",
                actualString: entry.Message);
            Assert.IsType<InvalidOperationException>(entry.Exception);
        }

        private static ProcessedIntegrationMessageCleanupHostedService CreateService(
            TestServiceScopeFactory scopeFactory,
            TimeProvider timeProvider,
            ProcessedIntegrationMessageCleanupOptions options,
            TestLogger<ProcessedIntegrationMessageCleanupHostedService> logger)
        {
            return new ProcessedIntegrationMessageCleanupHostedService(
                scopeFactory: scopeFactory,
                timeProvider: timeProvider,
                options: Microsoft.Extensions.Options.Options.Create(options),
                logger: logger);
        }

        private static async Task WaitUntilAsync(
            Func<bool> condition,
            TimeSpan timeout)
        {
            long startedAt = Stopwatch.GetTimestamp();

            while (Stopwatch.GetElapsedTime(startedAt) < timeout)
            {
                if (condition())
                    return;

                await Task.Delay(50);
            }

            Assert.True(
                condition: condition(),
                userMessage: "Timed out waiting for condition.");
        }
    }
}
