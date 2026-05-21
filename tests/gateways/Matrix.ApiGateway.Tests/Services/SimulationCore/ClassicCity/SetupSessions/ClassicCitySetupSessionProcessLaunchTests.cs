using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.Identity.Contracts.Internal.Responses;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions;

public sealed class ClassicCitySetupSessionProcessLaunchTests
{
    [Fact]
    public async Task ProcessLaunchAsync_WhenLaunchRequestIsMissing_FailsSession()
    {
        Guid ownerUserId = Guid.Parse("2b54eb4f-f14b-48fc-bd72-6d428a3707b0");
        Guid sessionId = Guid.Parse("5129b05c-d267-4ccf-bca7-2b850384ddfc");
        DateTimeOffset now = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint();
        ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId,
            status: "LaunchQueued");
        session.LaunchQueuedAtUtc = now.AddMinutes(-1);
        sessionStore.Sessions[sessionId] = session;
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
            timeProvider: CreateTimeProvider(now));

        await service.ProcessLaunchAsync(sessionId);

        Assert.Equal("LaunchFailed", session.Status);
        Assert.Equal("Gateway.ClassicCitySetup.LaunchRequestMissing", session.FailureCode);
        Assert.Equal(now, session.CompletedAtUtc);
        Assert.Equal(now, session.UpdatedAtUtc);
        Assert.Equal(1, sessionStore.SaveCallCount);
        Assert.Equal(1, sessionStore.ReleaseLockCallCount);
    }

    [Fact]
    public async Task ProcessLaunchAsync_WhenProvisioningSucceeds_CreatesLaunchSnapshotAndFinalizesSession()
    {
        Guid ownerUserId = Guid.Parse("bfdb4e6d-9e0e-4cc1-9112-a8702935b0ba");
        Guid sessionId = Guid.Parse("d1bef0bb-dde6-48f6-94aa-7b304a1e4011");
        Guid cityId = Guid.Parse("bb5157bd-b2f4-4c7b-bbb6-42d0e7bb93e7");
        DateTimeOffset now = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint();
        var permissionsVersionStore = new FakePermissionsVersionStore
        {
            CurrentVersion = 23
        };
        var authContextStore = new FakeAuthContextStore();
        authContextStore.Responses[(ownerUserId, 23)] = new UserAuthContextResponse(
            PermissionsVersion: 23,
            EffectivePermissions: ["city.launch", "", "city.launch", "city.view"]);
        var requestContextAccessor = new InternalJwtRequestContextAccessor();
        var provisioningService = new RecordingProvisioningService(requestContextAccessor)
        {
            CreateCityResult = CreateCityProvisioningView(cityId: cityId)
        };
        ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId,
            status: "LaunchQueued");
        session.LaunchQueuedAtUtc = now.AddMinutes(-1);
        session.LaunchRequest = CreateCityLaunchRequest(provisioningCorrelationId: sessionId);
        sessionStore.Sessions[sessionId] = session;
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId, "ignored-jti"),
            permissionsVersionStore: permissionsVersionStore,
            authContextStore: authContextStore,
            provisioningService: provisioningService,
            internalJwtRequestContextAccessor: requestContextAccessor,
            timeProvider: CreateTimeProvider(now));

        await service.ProcessLaunchAsync(sessionId);

        Assert.Equal("Ready", session.Status);
        Assert.Equal(cityId, session.CityId);
        Assert.Equal(now, session.StartedAtUtc);
        Assert.Equal(now, session.CompletedAtUtc);
        Assert.Equal(now, session.UpdatedAtUtc);
        Assert.NotNull(session.Provisioning);
        Assert.NotNull(session.LaunchAuthContext);
        Assert.Equal(ownerUserId, session.LaunchAuthContext!.UserId);
        Assert.Equal(23, session.LaunchAuthContext.PermissionsVersion);
        Assert.Equal(["city.launch", "city.view"], session.LaunchAuthContext.EffectivePermissions);
        Assert.Equal(now, session.LaunchAuthContext.CapturedAtUtc);
        Assert.Equal(ownerUserId, provisioningService.CapturedRequestContext!.UserId);
        Assert.Equal(["city.launch", "city.view"], provisioningService.CapturedRequestContext.EffectivePermissions);
        Assert.Equal(sessionId, provisioningService.LastCreateCityRequest!.ProvisioningCorrelationId);
        Assert.Equal(1, permissionsVersionStore.GetCurrentCallCount);
        Assert.Equal(1, authContextStore.GetCallCount);
        Assert.Equal(2, sessionStore.SaveCallCount);
    }

    [Fact]
    public async Task ProcessLaunchAsync_WhenProvisioningTransportFails_MarksLaunchFailed()
    {
        Guid ownerUserId = Guid.Parse("e58f7154-c790-4355-a169-89e5b6f25b4f");
        Guid sessionId = Guid.Parse("76960679-77d4-45b8-b057-f1a0f2eb9df8");
        DateTimeOffset now = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint();
        var requestContextAccessor = new InternalJwtRequestContextAccessor();
        var provisioningService = new RecordingProvisioningService(requestContextAccessor)
        {
            CreateCityException = new HttpRequestException("transport timeout")
        };
        ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId,
            status: "LaunchQueued");
        session.LaunchQueuedAtUtc = now.AddMinutes(-1);
        session.LaunchRequest = CreateCityLaunchRequest(provisioningCorrelationId: sessionId);
        session.LaunchAuthContext = CreateLaunchAuthSnapshot(ownerUserId);
        sessionStore.Sessions[sessionId] = session;
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
            provisioningService: provisioningService,
            internalJwtRequestContextAccessor: requestContextAccessor,
            timeProvider: CreateTimeProvider(now));

        await service.ProcessLaunchAsync(sessionId);

        Assert.Equal("LaunchFailed", session.Status);
        Assert.Equal("Gateway.ClassicCitySetup.CityCreateTransportError", session.FailureCode);
        Assert.Contains("transport timeout", session.FailureMessage);
        Assert.Equal(now, session.StartedAtUtc);
        Assert.Equal(now, session.CompletedAtUtc);
        Assert.Equal(now, session.UpdatedAtUtc);
        Assert.Equal(2, sessionStore.SaveCallCount);
    }
}
