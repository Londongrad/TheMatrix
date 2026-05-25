using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.Identity.Contracts.Internal.Responses;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionQueueLaunchTests
    {
        [Fact]
        public async Task QueueLaunchAsync_WhenDraftIsInvalid_ReturnsInvalidAndPersistsNormalizedDraft()
        {
            var ownerUserId = Guid.Parse("c2b71fa3-9c59-4516-9d03-968fbe06b30d");
            var sessionId = Guid.Parse("3203949c-a3fb-4a6b-b3a1-aadf030efaf0");
            DateTimeOffset now = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                draft: CreateClassicCitySetupDraft(
                    name: "",
                    generationSeed: " "));
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
                timeProvider: CreateTimeProvider(now));

            ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Invalid,
                actual: result.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.ValidationFailed",
                actual: result.ErrorCode);
            Assert.StartsWith(
                expectedStartString: "cc-",
                actualString: sessionStore.Sessions[sessionId].Draft.GenerationSeed);
            Assert.Equal(
                expected: now,
                actual: sessionStore.Sessions[sessionId].UpdatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.SaveCallCount);
            Assert.Empty(publishEndpoint.PublishedMessages);
        }

        [Fact]
        public async Task QueueLaunchAsync_WhenAuthSnapshotCaptureFails_ReturnsUnavailable()
        {
            var ownerUserId = Guid.Parse("8322152a-2616-4a33-80eb-a5eef7eb49fd");
            var sessionId = Guid.Parse("8c9c4890-6ff7-4b7c-b85f-4c8f9b0cc093");
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

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Unavailable,
                actual: result.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.LaunchAuthContextUnavailable",
                actual: result.ErrorCode);
            Assert.Equal(
                expected: "Draft",
                actual: sessionStore.Sessions[sessionId].Status);
            Assert.Equal(
                expected: 0,
                actual: sessionStore.SaveCallCount);
            Assert.Empty(publishEndpoint.PublishedMessages);
        }

        [Fact]
        public async Task QueueLaunchAsync_WhenDraftIsValid_QueuesLaunchAndPublishesMessage()
        {
            var ownerUserId = Guid.Parse("175515c1-d54f-4d8b-82e7-02f58dc17611");
            var sessionId = Guid.Parse("1b7807ff-4468-471c-942d-2ee40bbdb6fc");
            DateTimeOffset now = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            var permissionsVersionStore = new FakePermissionsVersionStore
            {
                CurrentVersion = 17
            };
            var authContextStore = new FakeAuthContextStore();
            authContextStore.Responses[(ownerUserId, 17)] = new UserAuthContextResponse(
                PermissionsVersion: 17,
                EffectivePermissions:
                [
                    "city.launch",
                    "city.launch",
                    "",
                    "city.view"
                ]);
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                draft: CreateClassicCitySetupDraft());
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(
                    userId: ownerUserId,
                    jti: "  launch-jti  "),
                permissionsVersionStore: permissionsVersionStore,
                authContextStore: authContextStore,
                timeProvider: CreateTimeProvider(now));

            ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

            ClassicCitySetupSessionState session = sessionStore.Sessions[sessionId];

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Updated,
                actual: result.Status);
            Assert.Equal(
                expected: "LaunchQueued",
                actual: session.Status);
            Assert.Equal(
                expected: "launch",
                actual: session.CurrentStepId);
            Assert.Equal(
                expected: now,
                actual: session.LaunchQueuedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.UpdatedAtUtc);
            Assert.NotNull(session.LaunchRequest);
            Assert.Equal(
                expected: sessionId,
                actual: session.LaunchRequest!.ProvisioningCorrelationId);
            Assert.Equal(
                expected: ownerUserId,
                actual: session.LaunchAuthContext!.UserId);
            Assert.Equal(
                expected: "launch-jti",
                actual: session.LaunchAuthContext.Jti);
            Assert.Equal(
                expected: 17,
                actual: session.LaunchAuthContext.PermissionsVersion);
            Assert.Equal(
                expectedSpan:
                [
                    "city.launch",
                    "city.view"
                ],
                actualArray: session.LaunchAuthContext.EffectivePermissions);
            Assert.Equal(
                expected: now,
                actual: session.LaunchAuthContext.CapturedAtUtc);
            ClassicCitySetupLaunchRequested published =
                Assert.IsType<ClassicCitySetupLaunchRequested>(Assert.Single(publishEndpoint.PublishedMessages));
            Assert.Equal(
                expected: sessionId,
                actual: published.SessionId);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.SaveCallCount);
        }

        [Fact]
        public async Task QueueLaunchAsync_WhenPublishFails_ReturnsUnavailableAndMarksLaunchFailed()
        {
            var ownerUserId = Guid.Parse("8f07f4c5-9a6c-4952-b1be-21823689f447");
            var sessionId = Guid.Parse("fd0ea4ce-67a4-42dd-ad71-56c3df560af3");
            DateTimeOffset now = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
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
                httpContextAccessor: CreateHttpContextAccessor(
                    userId: ownerUserId,
                    jti: "launch-jti"),
                permissionsVersionStore: permissionsVersionStore,
                authContextStore: authContextStore,
                timeProvider: CreateTimeProvider(now));

            ClassicCitySetupSessionMutationResult result = await service.QueueLaunchAsync(sessionId);

            ClassicCitySetupSessionState session = sessionStore.Sessions[sessionId];

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Unavailable,
                actual: result.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.LaunchQueueUnavailable",
                actual: result.ErrorCode);
            Assert.Equal(
                expected: "LaunchFailed",
                actual: session.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.LaunchQueueUnavailable",
                actual: session.FailureCode);
            Assert.Contains(
                expectedSubstring: "broker is down",
                actualString: session.FailureMessage);
            Assert.Equal(
                expected: now,
                actual: session.LaunchQueuedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.LaunchAuthContext!.CapturedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.CompletedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.UpdatedAtUtc);
            Assert.Equal(
                expected: 2,
                actual: sessionStore.SaveCallCount);
        }
    }
}
