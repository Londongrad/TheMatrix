using Matrix.Identity.Infrastructure.Security.Audit.Cleanup;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Audit.Cleanup
{
    public sealed class SecurityAuditCleanupHostedServiceTests
    {
        [Fact]
        public async Task StartAsync_WhenPollIntervalIsInvalid_LogsErrorAndStops()
        {
            var logger = new TestLogger<SecurityAuditCleanupHostedService>();
            var service = new SecurityAuditCleanupHostedService(
                scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
                options: Options.Create(
                    new SecurityAuditCleanupOptions
                    {
                        CleanupEnabled = true,
                        PollIntervalSeconds = 0,
                        BatchSize = 1
                    }),
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
        public async Task StartAsync_WhenTickDeletesEvents_LogsDebug()
        {
            var bulkRepository = new FakeSecurityAuditBulkRepository
            {
                DeleteBatchResult = 4
            };
            var cleaner = new SecurityAuditCleaner(
                securityAuditBulkRepository: bulkRepository,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));
            var logger = new TestLogger<SecurityAuditCleanupHostedService>();
            var service = new SecurityAuditCleanupHostedService(
                scopeFactory: new TestServiceScopeFactory(
                    new DictionaryServiceProvider(
                        new Dictionary<Type, object>
                        {
                            [typeof(SecurityAuditCleaner)] = cleaner
                        })),
                options: Options.Create(
                    new SecurityAuditCleanupOptions
                    {
                        CleanupEnabled = true,
                        PollIntervalSeconds = 1,
                        BatchSize = 10,
                        RetentionDays = 30
                    }),
                logger: logger);

            await service.StartAsync(CancellationToken.None);
            await WaitUntilAsync(
                condition: () => logger.Entries.Any(x => x.LogLevel == LogLevel.Debug),
                timeout: TimeSpan.FromSeconds(3));
            await service.StopAsync(CancellationToken.None);

            TestLogEntry entry = Assert.Single(
                logger.Entries.Where(x => x.LogLevel == LogLevel.Debug)
                   .Take(1));
            Assert.Contains(
                expectedSubstring: "Deleted 4 security audit events",
                actualString: entry.Message);
        }
    }
}
