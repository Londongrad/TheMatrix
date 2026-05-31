using System.Net;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionReconciliationTests
    {
        [Fact]
        public async Task ReconcileAsync_WhenSessionIsMissing_UntracksSession()
        {
            var sessionId = Guid.Parse("0e730d60-c2a4-40a4-9730-c9e7612357b5");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.TrackedSessionIds.Add(sessionId);
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(Guid.Parse("55d947f1-4dd2-44b8-a9bc-b6752bf0e340")));

            await service.ReconcileAsync(sessionId);

            Assert.Equal(
                expected: 1,
                actual: sessionStore.UntrackCallCount);
            Assert.Contains(
                expected: sessionId,
                collection: sessionStore.UntrackedSessionIds);
            Assert.DoesNotContain(
                expected: sessionId,
                set: sessionStore.TrackedSessionIds);
        }

        [Fact]
        public async Task ReconcileAsync_WhenSessionIsAlreadyReady_StopsTracking()
        {
            var ownerUserId = Guid.Parse("d59909f2-f050-4d58-a4af-6b9b0590e7d2");
            var sessionId = Guid.Parse("889d7ea5-edeb-4b63-9f95-d74b831210a2");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.TrackedSessionIds.Add(sessionId);
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "Ready");
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            await service.ReconcileAsync(sessionId);

            Assert.Equal(
                expected: 1,
                actual: sessionStore.UntrackCallCount);
            Assert.DoesNotContain(
                expected: sessionId,
                set: sessionStore.TrackedSessionIds);
            Assert.Equal(
                expected: 0,
                actual: sessionStore.SaveCallCount);
        }

        [Fact]
        public async Task ReconcileAsync_WhenQueuedLaunchIsStale_ReplaysProvisioning()
        {
            var ownerUserId = Guid.Parse("9eb87c61-06af-4138-b71f-f77ea15f621e");
            var sessionId = Guid.Parse("c9aa3a35-863a-4f2f-9209-9f29a891f8c9");
            var cityId = Guid.Parse("32f56e93-8871-47c4-bf77-86a1c362c78c");
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
                CreateCityResult = CreateCityProvisioningView(cityId: cityId)
            };
            ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "LaunchQueued",
                updatedAtUtc: now.AddMinutes(-1));
            session.LaunchQueuedAtUtc = now.AddMinutes(-1);
            session.LaunchRequest = CreateCityLaunchRequest(provisioningCorrelationId: sessionId);
            session.LaunchAuthContext = CreateLaunchAuthSnapshot(ownerUserId);
            sessionStore.TrackedSessionIds.Add(sessionId);
            sessionStore.Sessions[sessionId] = session;
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
                provisioningService: provisioningService,
                internalJwtRequestContextAccessor: requestContextAccessor,
                options: CreateClassicCitySetupSessionOptions(launchQueueRecoveryDelaySeconds: 1),
                timeProvider: CreateTimeProvider(now));

            await service.ReconcileAsync(sessionId);

            Assert.Equal(
                expected: 1,
                actual: provisioningService.CreateCityCallCount);
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
            Assert.Equal(
                expected: 0,
                actual: sessionStore.UntrackCallCount);
        }

        [Fact]
        public async Task ReconcileAsync_WhenProvisionedCityIsMissing_MarksProvisioningFailed()
        {
            var ownerUserId = Guid.Parse("33dd4a4c-85e5-4d9e-a2e3-8b1a2632a4d4");
            var sessionId = Guid.Parse("f4642b80-847d-421b-91c8-845c4808690d");
            var cityId = Guid.Parse("0f8a6616-a3bd-4f6d-b636-d3617c26f613");
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
            var citiesApiClient = new RecordingCitiesApiClient(requestContextAccessor)
            {
                ProvisioningStatusException = new DownstreamServiceException(
                    serviceName: "simulationcore",
                    statusCode: HttpStatusCode.NotFound,
                    body: "{\"error\":\"city missing\"}",
                    contentType: "application/json",
                    requestUrl: "https://simulationcore/cities/" + cityId)
            };
            ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "BootstrappingPopulation");
            session.CityId = cityId;
            session.LaunchAuthContext = CreateLaunchAuthSnapshot(ownerUserId);
            sessionStore.TrackedSessionIds.Add(sessionId);
            sessionStore.Sessions[sessionId] = session;
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
                citiesApiClient: citiesApiClient,
                internalJwtRequestContextAccessor: requestContextAccessor,
                timeProvider: CreateTimeProvider(now));

            await service.ReconcileAsync(sessionId);

            Assert.Equal(
                expected: "ProvisioningFailed",
                actual: session.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.ReconciliationCityNotFound",
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
                expected: 0,
                actual: sessionStore.UntrackCallCount);
        }

        [Fact]
        public async Task ReconcileAsync_WhenProvisioningIsActive_MarksSessionReadyAndUntracks()
        {
            var ownerUserId = Guid.Parse("b2435d64-84d7-47bd-a50a-2d748462db7f");
            var sessionId = Guid.Parse("a702cd92-79a9-4a33-a1e7-b5909f1af6c6");
            var cityId = Guid.Parse("8bc5a737-fd0d-49f1-a8bd-4ceff5c2cc55");
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
            var citiesApiClient = new RecordingCitiesApiClient(requestContextAccessor)
            {
                ProvisioningStatusResult = CreateCityProvisioningStatusView(
                    cityId: cityId,
                    status: "Active")
            };
            ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "BootstrappingPopulation");
            session.CityId = cityId;
            session.LaunchRequest = CreateCityLaunchRequest(provisioningCorrelationId: sessionId);
            session.LaunchAuthContext = CreateLaunchAuthSnapshot(
                userId: ownerUserId,
                permissionsVersion: 19,
                effectivePermissions:
                [
                    "city.view",
                    "city.launch"
                ]);
            sessionStore.TrackedSessionIds.Add(sessionId);
            sessionStore.Sessions[sessionId] = session;
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
                citiesApiClient: citiesApiClient,
                internalJwtRequestContextAccessor: requestContextAccessor,
                timeProvider: CreateTimeProvider(now));

            await service.ReconcileAsync(sessionId);

            Assert.Equal(
                expected: "Ready",
                actual: session.Status);
            Assert.Null(session.FailureCode);
            Assert.NotNull(session.Provisioning);
            Assert.Equal(
                expected: "Completed",
                actual: session.Provisioning!.PopulationBootstrap.Status);
            Assert.Equal(
                expected: "Completed",
                actual: session.Provisioning.EconomyBootstrap.Status);
            Assert.Equal(
                expected: now,
                actual: session.UpdatedAtUtc);
            Assert.Equal(
                expected: ownerUserId,
                actual: citiesApiClient.CapturedRequestContext!.UserId);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.UntrackCallCount);
            Assert.DoesNotContain(
                expected: sessionId,
                set: sessionStore.TrackedSessionIds);
        }
    }
}
