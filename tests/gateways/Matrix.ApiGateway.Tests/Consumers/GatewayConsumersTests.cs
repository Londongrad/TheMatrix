using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Consumers;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Tests.TestSupport;
using Matrix.Identity.Contracts.Internal.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.ApiGateway.Tests.Consumers
{
    public sealed class GatewayConsumersTests
    {
        [Fact]
        public async Task ConsumeAsync_WhenSetupLaunchIsRequested_ForwardsSessionIdToSetupService()
        {
            var setupSessionService = new ApiGatewayTestSupport.RecordingClassicCitySetupSessionService();
            var consumer = new ClassicCitySetupLaunchRequestedConsumer(setupSessionService);
            var sessionId = Guid.Parse("5d8de8fc-d23d-41f8-bcdc-c0beff4a94d6");

            await consumer.ConsumeAsync(
                message: new ClassicCitySetupLaunchRequested(sessionId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: sessionId,
                actual: setupSessionService.LastProcessLaunchSessionId);
        }

        [Fact]
        public async Task ConsumeAsync_WhenDefaultUserAccessPolicyChanges_WritesFreshAndStaleVersionEntries()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var consumer = new DefaultUserAccessPolicyChangedConsumer(
                cache: cache,
                options: ApiGatewayTestSupport.CreatePermissionsVersionOptions(
                    cacheTtlSeconds: 120,
                    staleCacheTtlSeconds: 600),
                logger: NullLogger<DefaultUserAccessPolicyChangedConsumer>.Instance);

            await consumer.ConsumeAsync(
                message: new DefaultUserAccessPolicyChangedV1(11),
                cancellationToken: CancellationToken.None);

            string freshKey = AuthorizationCacheKeys.DefaultUserAccessVersion();
            string staleKey = AuthorizationCacheKeys.DefaultUserAccessVersionStale();

            Assert.Equal(
                expected: "11",
                actual: cache.ReadString(freshKey));
            Assert.Equal(
                expected: "11",
                actual: cache.ReadString(staleKey));
            Assert.Equal(
                expected: TimeSpan.FromSeconds(120),
                actual: cache.WrittenOptions[freshKey].AbsoluteExpirationRelativeToNow);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(600),
                actual: cache.WrittenOptions[staleKey].AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public async Task ConsumeAsync_WhenDefaultUserAccessPolicyTtlsAreInvalid_UsesFallbackTtls()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var consumer = new DefaultUserAccessPolicyChangedConsumer(
                cache: cache,
                options: ApiGatewayTestSupport.CreatePermissionsVersionOptions(
                    cacheTtlSeconds: 0,
                    staleCacheTtlSeconds: -5),
                logger: NullLogger<DefaultUserAccessPolicyChangedConsumer>.Instance);

            await consumer.ConsumeAsync(
                message: new DefaultUserAccessPolicyChangedV1(19),
                cancellationToken: CancellationToken.None);

            string freshKey = AuthorizationCacheKeys.DefaultUserAccessVersion();
            string staleKey = AuthorizationCacheKeys.DefaultUserAccessVersionStale();

            Assert.Equal(
                expected: TimeSpan.FromSeconds(1800),
                actual: cache.WrittenOptions[freshKey].AbsoluteExpirationRelativeToNow);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(1800),
                actual: cache.WrittenOptions[staleKey].AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public async Task ConsumeAsync_WhenUserSecurityStateChanges_WritesPermissionsVersionEntry()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var consumer = new UserSecurityStateChangedConsumer(
                cache: cache,
                options: ApiGatewayTestSupport.CreatePermissionsVersionOptions(cacheTtlSeconds: 90),
                logger: NullLogger<UserSecurityStateChangedConsumer>.Instance);
            var userId = Guid.Parse("c8c530b2-e2c3-418c-9d2b-6ebc3c8d480a");

            await consumer.ConsumeAsync(
                message: new UserSecurityStateChangedV1(
                    UserId: userId,
                    PermissionsVersion: 27),
                cancellationToken: CancellationToken.None);

            string key = AuthorizationCacheKeys.PermissionsVersion(userId);

            Assert.Equal(
                expected: "27",
                actual: cache.ReadString(key));
            Assert.Equal(
                expected: TimeSpan.FromSeconds(90),
                actual: cache.WrittenOptions[key].AbsoluteExpirationRelativeToNow);
        }

        [Fact]
        public async Task ConsumeAsync_WhenUserSecurityStateTtlIsInvalid_UsesFallbackTtl()
        {
            var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
            var consumer = new UserSecurityStateChangedConsumer(
                cache: cache,
                options: ApiGatewayTestSupport.CreatePermissionsVersionOptions(cacheTtlSeconds: 0),
                logger: NullLogger<UserSecurityStateChangedConsumer>.Instance);
            var userId = Guid.Parse("d8b9e8aa-2976-4eb1-8e46-5e7edfc39d31");

            await consumer.ConsumeAsync(
                message: new UserSecurityStateChangedV1(
                    UserId: userId,
                    PermissionsVersion: 34),
                cancellationToken: CancellationToken.None);

            string key = AuthorizationCacheKeys.PermissionsVersion(userId);

            Assert.Equal(
                expected: TimeSpan.FromSeconds(1800),
                actual: cache.WrittenOptions[key].AbsoluteExpirationRelativeToNow);
        }
    }
}
