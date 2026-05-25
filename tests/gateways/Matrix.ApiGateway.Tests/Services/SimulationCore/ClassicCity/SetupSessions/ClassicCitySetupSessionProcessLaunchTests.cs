using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.Identity.Contracts.Internal.Responses;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionProcessLaunchTests
    {
        [Fact]
        public async Task ProcessLaunchAsync_WhenLaunchRequestIsMissing_FailsSession()
        {
            var ownerUserId = Guid.Parse("2b54eb4f-f14b-48fc-bd72-6d428a3707b0");
            var sessionId = Guid.Parse("5129b05c-d267-4ccf-bca7-2b850384ddfc");
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

            Assert.Equal(
                expected: "LaunchFailed",
                actual: session.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.LaunchRequestMissing",
                actual: session.FailureCode);
            Assert.Equal(
                expected: now,
                actual: session.CompletedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.UpdatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.SaveCallCount);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.ReleaseLockCallCount);
        }

        [Fact]
        public async Task ProcessLaunchAsync_WhenProvisioningSucceeds_CreatesLaunchSnapshotAndFinalizesSession()
        {
            var ownerUserId = Guid.Parse("bfdb4e6d-9e0e-4cc1-9112-a8702935b0ba");
            var sessionId = Guid.Parse("d1bef0bb-dde6-48f6-94aa-7b304a1e4011");
            var cityId = Guid.Parse("bb5157bd-b2f4-4c7b-bbb6-42d0e7bb93e7");
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
                CurrentVersion = 23
            };
            var authContextStore = new FakeAuthContextStore();
            authContextStore.Responses[(ownerUserId, 23)] = new UserAuthContextResponse(
                PermissionsVersion: 23,
                EffectivePermissions:
                [
                    "city.launch",
                    "",
                    "city.launch",
                    "city.view"
                ]);
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
                httpContextAccessor: CreateHttpContextAccessor(
                    userId: ownerUserId,
                    jti: "ignored-jti"),
                permissionsVersionStore: permissionsVersionStore,
                authContextStore: authContextStore,
                provisioningService: provisioningService,
                internalJwtRequestContextAccessor: requestContextAccessor,
                timeProvider: CreateTimeProvider(now));

            await service.ProcessLaunchAsync(sessionId);

            Assert.Equal(
                expected: "Ready",
                actual: session.Status);
            Assert.Equal(
                expected: cityId,
                actual: session.CityId);
            Assert.Equal(
                expected: now,
                actual: session.StartedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.CompletedAtUtc);
            Assert.Equal(
                expected: now,
                actual: session.UpdatedAtUtc);
            Assert.NotNull(session.Provisioning);
            Assert.NotNull(session.LaunchAuthContext);
            Assert.Equal(
                expected: ownerUserId,
                actual: session.LaunchAuthContext!.UserId);
            Assert.Equal(
                expected: 23,
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
            Assert.Equal(
                expected: ownerUserId,
                actual: provisioningService.CapturedRequestContext!.UserId);
            Assert.Equal(
                expectedSpan:
                [
                    "city.launch",
                    "city.view"
                ],
                actualArray: provisioningService.CapturedRequestContext.EffectivePermissions);
            Assert.Equal(
                expected: sessionId,
                actual: provisioningService.LastCreateCityRequest!.ProvisioningCorrelationId);
            Assert.Equal(
                expected: 1,
                actual: permissionsVersionStore.GetCurrentCallCount);
            Assert.Equal(
                expected: 1,
                actual: authContextStore.GetCallCount);
            Assert.Equal(
                expected: 2,
                actual: sessionStore.SaveCallCount);
        }

        [Fact]
        public async Task ProcessLaunchAsync_WhenProvisioningTransportFails_MarksLaunchFailed()
        {
            var ownerUserId = Guid.Parse("e58f7154-c790-4355-a169-89e5b6f25b4f");
            var sessionId = Guid.Parse("76960679-77d4-45b8-b057-f1a0f2eb9df8");
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

            Assert.Equal(
                expected: "LaunchFailed",
                actual: session.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.CityCreateTransportError",
                actual: session.FailureCode);
            Assert.Contains(
                expectedSubstring: "transport timeout",
                actualString: session.FailureMessage);
            Assert.Equal(
                expected: now,
                actual: session.StartedAtUtc);
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
