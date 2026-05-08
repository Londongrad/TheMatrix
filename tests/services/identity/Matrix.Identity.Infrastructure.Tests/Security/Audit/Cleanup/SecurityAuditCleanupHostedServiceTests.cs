using Matrix.Identity.Infrastructure.Security.Audit.Cleanup;
using Microsoft.Extensions.Logging;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Audit.Cleanup;

public sealed class SecurityAuditCleanupHostedServiceTests
{
    [Fact]
    public async Task StartAsync_WhenPollIntervalIsInvalid_LogsErrorAndStops()
    {
        var logger = new TestLogger<SecurityAuditCleanupHostedService>();
        var service = new SecurityAuditCleanupHostedService(
            scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
            options: Microsoft.Extensions.Options.Options.Create(
                new SecurityAuditCleanupOptions
                {
                    CleanupEnabled = true,
                    PollIntervalSeconds = 0,
                    BatchSize = 1
                }),
            logger: logger);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries, x => x.LogLevel == LogLevel.Error);
        Assert.Contains("poll interval must be > 0", entry.Message);
    }

    [Fact]
    public async Task StartAsync_WhenTickDeletesEvents_LogsDebug()
    {
        var bulkRepository = new FakeSecurityAuditBulkRepository
        {
            DeleteBatchResult = 4
        };
        var cleaner = new SecurityAuditCleaner(
            bulkRepository,
            CreateTimeProvider(new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero)));
        var logger = new TestLogger<SecurityAuditCleanupHostedService>();
        var service = new SecurityAuditCleanupHostedService(
            scopeFactory: new TestServiceScopeFactory(
                new DictionaryServiceProvider(
                    new Dictionary<Type, object>
                    {
                        [typeof(SecurityAuditCleaner)] = cleaner
                    })),
            options: Microsoft.Extensions.Options.Options.Create(
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
            () => logger.Entries.Any(x => x.LogLevel == LogLevel.Debug),
            TimeSpan.FromSeconds(3));
        await service.StopAsync(CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries.Where(x => x.LogLevel == LogLevel.Debug).Take(1));
        Assert.Contains("Deleted 4 security audit events", entry.Message);
    }
}
