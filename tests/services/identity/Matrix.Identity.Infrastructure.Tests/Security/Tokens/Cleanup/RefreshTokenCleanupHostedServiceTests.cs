using Matrix.Identity.Infrastructure.Security.Tokens.Cleanup;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens.Cleanup
{
    public sealed class RefreshTokenCleanupHostedServiceTests
    {
        [Fact]
        public async Task StartAsync_WhenCleanupIsDisabled_LogsInformationAndStops()
        {
            var logger = new TestLogger<RefreshTokenCleanupHostedService>();
            var service = new RefreshTokenCleanupHostedService(
                scopeFactory: new TestServiceScopeFactory(new DictionaryServiceProvider([])),
                options: Options.Create(
                    new RefreshTokenCleanupOptions
                    {
                        CleanupEnabled = false
                    }),
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
        public async Task StartAsync_WhenTickDeletesTokens_LogsDebug()
        {
            var bulkRepository = new FakeRefreshTokenBulkRepository
            {
                DeleteRevokedBatchResult = 2,
                DeleteExpiredBatchResult = 1
            };
            var cleaner = new RefreshTokenCleaner(
                refreshTokenBulkRepository: bulkRepository,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));
            var logger = new TestLogger<RefreshTokenCleanupHostedService>();
            var service = new RefreshTokenCleanupHostedService(
                scopeFactory: new TestServiceScopeFactory(
                    new DictionaryServiceProvider(
                        new Dictionary<Type, object>
                        {
                            [typeof(RefreshTokenCleaner)] = cleaner
                        })),
                options: Options.Create(
                    new RefreshTokenCleanupOptions
                    {
                        CleanupEnabled = true,
                        PollIntervalSeconds = 1,
                        BatchSize = 5
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
                expectedSubstring: "Deleted 2 revoked refresh tokens and 1 expired refresh tokens",
                actualString: entry.Message);
        }
    }
}
