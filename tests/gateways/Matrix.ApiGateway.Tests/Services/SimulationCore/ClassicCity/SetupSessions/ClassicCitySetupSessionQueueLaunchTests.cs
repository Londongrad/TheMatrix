using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.Identity.Contracts.Internal.Responses;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions;

public sealed class ClassicCitySetupSessionQueueLaunchTests
{
    [Fact]
    public async Task QueueLaunchAsync_WhenDraftIsInvalid_ReturnsInvalidAndPersistsNormalizedDraft()
    {
        Guid ownerUserId = Guid.Parse("c2b71fa3-9c59-4516-9d03-968fbe06b30d");
        Guid sessionId = Guid.Parse("3203949c-a3fb-4a6b-b3a1-aadf030efaf0");
        DateTimeOffset now = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint();
        sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId,
            draft: CreateClassicCitySetupDraft(name: "", generationSeed: " "));
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
            timeProvider: CreateTimeProvider(now));

        ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

        Assert.Equal(ClassicCitySetupSessionMutationStatus.Invalid, result.Status);
        Assert.Equal("Gateway.ClassicCitySetup.ValidationFailed", result.ErrorCode);
        Assert.StartsWith("cc-", sessionStore.Sessions[sessionId].Draft.GenerationSeed);
        Assert.Equal(now, sessionStore.Sessions[sessionId].UpdatedAtUtc);
        Assert.Equal(1, sessionStore.SaveCallCount);
        Assert.Empty(publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task QueueLaunchAsync_WhenAuthSnapshotCaptureFails_ReturnsUnavailable()
    {
        Guid ownerUserId = Guid.Parse("8322152a-2616-4a33-80eb-a5eef7eb49fd");
        Guid sessionId = Guid.Parse("8c9c4890-6ff7-4b7c-b85f-4c8f9b0cc093");
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint();
        var permissionsVersionStore = new FakePermissionsVersionStore
        {
            Exception = new InvalidOperationException("permissions service down")
        };
        sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId);
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
            permissionsVersionStore: permissionsVersionStore);

        ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

        Assert.Equal(ClassicCitySetupSessionMutationStatus.Unavailable, result.Status);
        Assert.Equal("Gateway.ClassicCitySetup.LaunchAuthContextUnavailable", result.ErrorCode);
        Assert.Equal("Draft", sessionStore.Sessions[sessionId].Status);
        Assert.Equal(0, sessionStore.SaveCallCount);
        Assert.Empty(publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task QueueLaunchAsync_WhenDraftIsValid_QueuesLaunchAndPublishesMessage()
    {
        Guid ownerUserId = Guid.Parse("175515c1-d54f-4d8b-82e7-02f58dc17611");
        Guid sessionId = Guid.Parse("1b7807ff-4468-471c-942d-2ee40bbdb6fc");
        DateTimeOffset now = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint();
        var permissionsVersionStore = new FakePermissionsVersionStore
        {
            CurrentVersion = 17
        };
        var authContextStore = new FakeAuthContextStore();
        authContextStore.Responses[(ownerUserId, 17)] = new UserAuthContextResponse(
            PermissionsVersion: 17,
            EffectivePermissions: ["city.launch", "city.launch", "", "city.view"]);
        sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId,
            draft: CreateClassicCitySetupDraft());
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId, "  launch-jti  "),
            permissionsVersionStore: permissionsVersionStore,
            authContextStore: authContextStore,
            timeProvider: CreateTimeProvider(now));

        ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

        ClassicCitySetupSessionState session = sessionStore.Sessions[sessionId];

        Assert.Equal(ClassicCitySetupSessionMutationStatus.Updated, result.Status);
        Assert.Equal("LaunchQueued", session.Status);
        Assert.Equal("launch", session.CurrentStepId);
        Assert.Equal(now, session.LaunchQueuedAtUtc);
        Assert.Equal(now, session.UpdatedAtUtc);
        Assert.NotNull(session.LaunchRequest);
        Assert.Equal(sessionId, session.LaunchRequest!.ProvisioningCorrelationId);
        Assert.Equal(ownerUserId, session.LaunchAuthContext!.UserId);
        Assert.Equal("launch-jti", session.LaunchAuthContext.Jti);
        Assert.Equal(17, session.LaunchAuthContext.PermissionsVersion);
        Assert.Equal(["city.launch", "city.view"], session.LaunchAuthContext.EffectivePermissions);
        Assert.Equal(now, session.LaunchAuthContext.CapturedAtUtc);
        ClassicCitySetupLaunchRequested published = Assert.IsType<ClassicCitySetupLaunchRequested>(Assert.Single(publishEndpoint.PublishedMessages));
        Assert.Equal(sessionId, published.SessionId);
        Assert.Equal(1, sessionStore.SaveCallCount);
    }

    [Fact]
    public async Task QueueLaunchAsync_WhenPublishFails_ReturnsUnavailableAndMarksLaunchFailed()
    {
        Guid ownerUserId = Guid.Parse("8f07f4c5-9a6c-4952-b1be-21823689f447");
        Guid sessionId = Guid.Parse("fd0ea4ce-67a4-42dd-ad71-56c3df560af3");
        DateTimeOffset now = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sessionStore = new FakeClassicCitySetupSessionStore();
        var publishEndpoint = new RecordingPublishEndpoint
        {
            Exception = new InvalidOperationException("broker is down")
        };
        var permissionsVersionStore = new FakePermissionsVersionStore
        {
            CurrentVersion = 5
        };
        var authContextStore = new FakeAuthContextStore();
        authContextStore.Responses[(ownerUserId, 5)] = new UserAuthContextResponse(
            PermissionsVersion: 5,
            EffectivePermissions: ["city.launch"]);
        sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
            sessionId: sessionId,
            ownerUserId: ownerUserId,
            draft: CreateClassicCitySetupDraft());
        ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
            sessionStore: sessionStore,
            publishEndpoint: publishEndpoint,
            httpContextAccessor: CreateHttpContextAccessor(ownerUserId, "launch-jti"),
            permissionsVersionStore: permissionsVersionStore,
            authContextStore: authContextStore,
            timeProvider: CreateTimeProvider(now));

        ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

        ClassicCitySetupSessionState session = sessionStore.Sessions[sessionId];

        Assert.Equal(ClassicCitySetupSessionMutationStatus.Unavailable, result.Status);
        Assert.Equal("Gateway.ClassicCitySetup.LaunchQueueUnavailable", result.ErrorCode);
        Assert.Equal("LaunchFailed", session.Status);
        Assert.Equal("Gateway.ClassicCitySetup.LaunchQueueUnavailable", session.FailureCode);
        Assert.Contains("broker is down", session.FailureMessage);
        Assert.Equal(now, session.LaunchQueuedAtUtc);
        Assert.Equal(now, session.LaunchAuthContext!.CapturedAtUtc);
        Assert.Equal(now, session.CompletedAtUtc);
        Assert.Equal(now, session.UpdatedAtUtc);
        Assert.Equal(2, sessionStore.SaveCallCount);
    }
}
