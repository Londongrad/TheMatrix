using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionDraftMutationTests
    {
        [Fact]
        public async Task ListDraftsAsync_FiltersToMutableStatusesAndOrdersDescending()
        {
            var ownerUserId = Guid.Parse("8809fd58-5912-46ca-b354-687a7dd8dd37");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[Guid.Parse("a9c8f275-f7e4-442d-b64b-b617123be5f4")] =
                CreateClassicCitySetupSessionState(
                    sessionId: Guid.Parse("a9c8f275-f7e4-442d-b64b-b617123be5f4"),
                    ownerUserId: ownerUserId,
                    status: "Draft",
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero));
            sessionStore.Sessions[Guid.Parse("2ddf18ff-30ce-4176-98de-8a0fca46267f")] =
                CreateClassicCitySetupSessionState(
                    sessionId: Guid.Parse("2ddf18ff-30ce-4176-98de-8a0fca46267f"),
                    ownerUserId: ownerUserId,
                    status: "LaunchFailed",
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 11,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero));
            sessionStore.Sessions[Guid.Parse("4f28db43-065c-4f0d-97ab-44ff6ab95ec7")] =
                CreateClassicCitySetupSessionState(
                    sessionId: Guid.Parse("4f28db43-065c-4f0d-97ab-44ff6ab95ec7"),
                    ownerUserId: ownerUserId,
                    status: "LaunchQueued",
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 12,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero));
            sessionStore.Sessions[Guid.Parse("0e54f44b-9cf2-48d1-8f08-0b3ce918f9cf")] =
                CreateClassicCitySetupSessionState(
                    sessionId: Guid.Parse("0e54f44b-9cf2-48d1-8f08-0b3ce918f9cf"),
                    ownerUserId: Guid.Parse("9c5664cb-4685-43ec-b0c2-61d5ef88af84"),
                    status: "Draft",
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 13,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero));
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            IReadOnlyList<ClassicCitySetupSessionView> result = await service.ListDraftsAsync();

            Assert.Equal(
                expectedSpan:
                [
                    Guid.Parse("2ddf18ff-30ce-4176-98de-8a0fca46267f"),
                    Guid.Parse("a9c8f275-f7e4-442d-b64b-b617123be5f4")
                ],
                actualArray: result.Select(x => x.SessionId)
                   .ToArray());
        }

        [Fact]
        public async Task CreateAsync_WhenRecentEquivalentDraftExists_ReusesExistingDraft()
        {
            var ownerUserId = Guid.Parse("1fa1c722-5aad-43c1-a905-f56ef6e3dd81");
            var existingSessionId = Guid.Parse("6ed188fe-b437-4cd1-b1fa-5172b30d64fe");
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
            sessionStore.Sessions[existingSessionId] = CreateClassicCitySetupSessionState(
                sessionId: existingSessionId,
                ownerUserId: ownerUserId,
                currentStepId: "profile",
                draft: CreateClassicCitySetupDraft(
                    generationSeed: "seed-old",
                    startSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 2,
                        hour: 8,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                updatedAtUtc: now.AddSeconds(-5));
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
                timeProvider: CreateTimeProvider(now));

            ClassicCitySetupSessionView result = await service.CreateAsync(
                new CreateClassicCitySetupSessionRequestDto(
                    CurrentStepId: "profile",
                    Draft: CreateClassicCitySetupDraft(
                        generationSeed: "seed-new",
                        startSimTimeUtc: new DateTimeOffset(
                            year: 2048,
                            month: 6,
                            day: 3,
                            hour: 9,
                            minute: 0,
                            second: 0,
                            offset: TimeSpan.Zero))));

            Assert.Equal(
                expected: existingSessionId,
                actual: result.SessionId);
            Assert.Equal(
                expected: 0,
                actual: sessionStore.SaveCallCount);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.ReleaseCreateLockCallCount);
        }

        [Fact]
        public async Task CreateAsync_WhenNoReusableDraftExists_SavesNormalizedDraft()
        {
            var ownerUserId = Guid.Parse("a47a7248-7916-430c-a031-1dddab08f4f0");
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
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId),
                timeProvider: CreateTimeProvider(now));

            ClassicCitySetupSessionView result = await service.CreateAsync(
                new CreateClassicCitySetupSessionRequestDto(
                    CurrentStepId: "unknown-step",
                    Draft: CreateClassicCitySetupDraft(generationSeed: " ")));

            Assert.Equal(
                expected: "scenario",
                actual: result.CurrentStepId);
            Assert.Equal(
                expected: "Draft",
                actual: result.Status);
            Assert.StartsWith(
                expectedStartString: "cc-",
                actualString: result.Draft.GenerationSeed);
            Assert.Equal(
                expected: now,
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: now,
                actual: result.UpdatedAtUtc);
            Assert.Equal(
                expected: ownerUserId,
                actual: sessionStore.Sessions[result.SessionId].OwnerUserId);
            Assert.Equal(
                expected: now,
                actual: sessionStore.Sessions[result.SessionId].CreatedAtUtc);
            Assert.Equal(
                expected: now,
                actual: sessionStore.Sessions[result.SessionId].UpdatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.SaveCallCount);
        }

        [Fact]
        public async Task UpdateAsync_WhenSessionIsNotMutable_ReturnsConflict()
        {
            var ownerUserId = Guid.Parse("fbd08de7-56f6-45aa-b9bc-1ab4145eddb5");
            var sessionId = Guid.Parse("badf16df-985e-4b32-a33c-fc5540e96de9");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "LaunchQueued");
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            ClassicCitySetupSessionMutationResult result = await service.UpdateAsync(
                sessionId: sessionId,
                request: new UpdateClassicCitySetupSessionRequestDto(
                    CurrentStepId: "launch",
                    Draft: CreateClassicCitySetupDraft(name: "Updated City")));

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Conflict,
                actual: result.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.InvalidLaunchState",
                actual: result.ErrorCode);
            Assert.Equal(
                expected: 0,
                actual: sessionStore.SaveCallCount);
        }

        [Fact]
        public async Task DeleteAsync_WhenOwnedDraftExists_DeletesSession()
        {
            var ownerUserId = Guid.Parse("3cb16a5d-9308-4ff2-b118-2eb89b063386");
            var sessionId = Guid.Parse("57b7a7b0-75f6-45b5-952d-966ea68272c5");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "Draft");
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            ClassicCitySetupSessionMutationResult result = await service.DeleteAsync(sessionId);

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Updated,
                actual: result.Status);
            Assert.Null(result.Session);
            Assert.Equal(
                expected: 1,
                actual: sessionStore.DeleteCallCount);
            Assert.False(sessionStore.Sessions.ContainsKey(sessionId));
        }

        [Fact]
        public async Task DeleteAsync_WhenSessionIsBusy_ReturnsConflict()
        {
            var ownerUserId = Guid.Parse("8da52c08-56b4-4bfb-86b0-8e31a2940648");
            var sessionId = Guid.Parse("4707d809-0de1-46b4-a612-c9b9f15438c9");
            var sessionStore = new FakeClassicCitySetupSessionStore
            {
                LockToReturn = null
            };
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "Draft");
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            ClassicCitySetupSessionMutationResult result = await service.DeleteAsync(sessionId);

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Conflict,
                actual: result.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.SessionBusy",
                actual: result.ErrorCode);
            Assert.Equal(
                expected: 0,
                actual: sessionStore.DeleteCallCount);
        }

        [Fact]
        public async Task UpdateAsync_WhenLockAcquisitionThrows_ReturnsUnavailable()
        {
            var ownerUserId = Guid.Parse("56cf0b45-78d5-4861-a7ad-bfb96f4b362d");
            var sessionId = Guid.Parse("f39a9d6d-c2c0-43bc-a7c7-4daab3226c6d");
            var sessionStore = new FakeClassicCitySetupSessionStore
            {
                TryAcquireLockException = new InvalidOperationException("redis unavailable")
            };
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "Draft");
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            ClassicCitySetupSessionMutationResult result = await service.UpdateAsync(
                sessionId: sessionId,
                request: new UpdateClassicCitySetupSessionRequestDto(
                    CurrentStepId: "profile",
                    Draft: CreateClassicCitySetupDraft(name: "Updated City")));

            Assert.Equal(
                expected: ClassicCitySetupSessionMutationStatus.Unavailable,
                actual: result.Status);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.SessionLockUnavailable",
                actual: result.ErrorCode);
            Assert.Equal(
                expected: 0,
                actual: sessionStore.SaveCallCount);
        }
    }
}
