using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Consumers;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Tests.TestSupport;
using Matrix.Identity.Contracts.Internal.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.ApiGateway.Tests.Consumers;

public sealed class GatewayConsumersTests
{
    [Fact]
    public async Task ConsumeAsync_WhenSetupLaunchIsRequested_ForwardsSessionIdToSetupService()
    {
        var setupSessionService = new ApiGatewayTestSupport.RecordingClassicCitySetupSessionService();
        var consumer = new ClassicCitySetupLaunchRequestedConsumer(setupSessionService);
        Guid sessionId = Guid.Parse("5d8de8fc-d23d-41f8-bcdc-c0beff4a94d6");

        await consumer.ConsumeAsync(
            message: new ClassicCitySetupLaunchRequested(sessionId),
            cancellationToken: CancellationToken.None);

        Assert.Equal(sessionId, setupSessionService.LastProcessLaunchSessionId);
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

        Assert.Equal("11", cache.ReadString(freshKey));
        Assert.Equal("11", cache.ReadString(staleKey));
        Assert.Equal(TimeSpan.FromSeconds(120), cache.WrittenOptions[freshKey].AbsoluteExpirationRelativeToNow);
        Assert.Equal(TimeSpan.FromSeconds(600), cache.WrittenOptions[staleKey].AbsoluteExpirationRelativeToNow);
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

        Assert.Equal(TimeSpan.FromSeconds(1800), cache.WrittenOptions[freshKey].AbsoluteExpirationRelativeToNow);
        Assert.Equal(TimeSpan.FromSeconds(1800), cache.WrittenOptions[staleKey].AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task ConsumeAsync_WhenUserSecurityStateChanges_WritesPermissionsVersionEntry()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var consumer = new UserSecurityStateChangedConsumer(
            cache: cache,
            options: ApiGatewayTestSupport.CreatePermissionsVersionOptions(cacheTtlSeconds: 90),
            logger: NullLogger<UserSecurityStateChangedConsumer>.Instance);
        Guid userId = Guid.Parse("c8c530b2-e2c3-418c-9d2b-6ebc3c8d480a");

        await consumer.ConsumeAsync(
            message: new UserSecurityStateChangedV1(userId, 27),
            cancellationToken: CancellationToken.None);

        string key = AuthorizationCacheKeys.PermissionsVersion(userId);

        Assert.Equal("27", cache.ReadString(key));
        Assert.Equal(TimeSpan.FromSeconds(90), cache.WrittenOptions[key].AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task ConsumeAsync_WhenUserSecurityStateTtlIsInvalid_UsesFallbackTtl()
    {
        var cache = new ApiGatewayTestSupport.RecordingDistributedCache();
        var consumer = new UserSecurityStateChangedConsumer(
            cache: cache,
            options: ApiGatewayTestSupport.CreatePermissionsVersionOptions(cacheTtlSeconds: 0),
            logger: NullLogger<UserSecurityStateChangedConsumer>.Instance);
        Guid userId = Guid.Parse("d8b9e8aa-2976-4eb1-8e46-5e7edfc39d31");

        await consumer.ConsumeAsync(
            message: new UserSecurityStateChangedV1(userId, 34),
            cancellationToken: CancellationToken.None);

        string key = AuthorizationCacheKeys.PermissionsVersion(userId);

        Assert.Equal(TimeSpan.FromSeconds(1800), cache.WrittenOptions[key].AbsoluteExpirationRelativeToNow);
    }
}
