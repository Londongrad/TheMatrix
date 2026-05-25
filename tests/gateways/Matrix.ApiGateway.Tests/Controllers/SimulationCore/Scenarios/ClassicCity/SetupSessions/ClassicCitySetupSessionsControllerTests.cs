using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionsControllerTests
    {
        [Fact]
        public async Task ListDrafts_WhenCalled_ReturnsOkWithSessions()
        {
            ClassicCitySetupSessionView session = CreateClassicCitySetupSessionView();
            var service = new RecordingClassicCitySetupSessionService
            {
                ListDraftsResult = [session]
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);

            ActionResult<IReadOnlyList<ClassicCitySetupSessionView>> actionResult =
                await controller.ListDrafts(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            IReadOnlyList<ClassicCitySetupSessionView> result =
                Assert.IsAssignableFrom<IReadOnlyList<ClassicCitySetupSessionView>>(ok.Value);
            Assert.Single(result);
            Assert.Equal(
                expected: session.SessionId,
                actual: result[0].SessionId);
        }

        [Fact]
        public async Task Create_WhenCalled_ReturnsCreatedAndPassesRequest()
        {
            ClassicCitySetupSessionView created = CreateClassicCitySetupSessionView(
                sessionId: Guid.Parse("5b5ff126-6de8-4454-870e-b80a11485d40"),
                currentStepId: "profile");
            var service = new RecordingClassicCitySetupSessionService
            {
                CreateResult = created
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);
            CreateClassicCitySetupSessionRequestDto request = new(
                CurrentStepId: "profile",
                Draft: CreateClassicCitySetupDraft(name: "Chita Prime"));

            ActionResult<ClassicCitySetupSessionView> actionResult = await controller.Create(
                request: request,
                cancellationToken: CancellationToken.None);

            CreatedResult createdResult = Assert.IsType<CreatedResult>(actionResult.Result);
            ClassicCitySetupSessionView result = Assert.IsType<ClassicCitySetupSessionView>(createdResult.Value);
            Assert.Equal(
                expected: created.SessionId,
                actual: result.SessionId);
            Assert.Equal(
                expected: $"/api/scenarios/classic-city/setup-sessions/{created.SessionId}",
                actual: createdResult.Location);
            Assert.Same(
                expected: request,
                actual: service.LastCreateRequest);
        }

        [Fact]
        public async Task Get_WhenSessionIsMissing_ReturnsNotFound()
        {
            var sessionId = Guid.Parse("ed8d33ff-0d9c-4a2b-bb10-d0c932797d03");
            var service = new RecordingClassicCitySetupSessionService
            {
                GetResult = null
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);

            ActionResult<ClassicCitySetupSessionView> actionResult = await controller.Get(
                sessionId: sessionId,
                cancellationToken: CancellationToken.None);

            Assert.IsType<NotFoundResult>(actionResult.Result);
            Assert.Equal(
                expected: sessionId,
                actual: service.LastGetSessionId);
        }

        [Fact]
        public async Task Delete_WhenServiceReturnsConflict_MapsConflictPayload()
        {
            var sessionId = Guid.Parse("41fe5a43-b608-4932-927b-ea7142307d68");
            var service = new RecordingClassicCitySetupSessionService
            {
                DeleteResult = new ClassicCitySetupSessionMutationResult(
                    Status: ClassicCitySetupSessionMutationStatus.Conflict,
                    Session: null,
                    ErrorCode: "Gateway.ClassicCitySetup.SessionBusy",
                    ErrorMessage: "Session is currently locked.")
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);

            IActionResult actionResult = await controller.Delete(
                sessionId: sessionId,
                cancellationToken: CancellationToken.None);

            ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(actionResult);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.SessionBusy",
                actual: GetAnonymousProperty<string>(
                    source: conflict.Value,
                    propertyName: "code"));
            Assert.Equal(
                expected: "Session is currently locked.",
                actual: GetAnonymousProperty<string>(
                    source: conflict.Value,
                    propertyName: "message"));
            Assert.Equal(
                expected: sessionId,
                actual: service.LastDeleteSessionId);
        }

        [Fact]
        public async Task Update_WhenServiceReturnsInvalid_MapsBadRequestPayload()
        {
            var sessionId = Guid.Parse("5d10e0b7-46f9-46dd-8183-12d7110e00ec");
            UpdateClassicCitySetupSessionRequestDto request = new(
                CurrentStepId: "launch",
                Draft: CreateClassicCitySetupDraft(name: "Launch City"));
            var service = new RecordingClassicCitySetupSessionService
            {
                UpdateResult = new ClassicCitySetupSessionMutationResult(
                    Status: ClassicCitySetupSessionMutationStatus.Invalid,
                    Session: null,
                    ErrorCode: "Gateway.ClassicCitySetup.InvalidPayload",
                    ErrorMessage: "Draft is invalid.")
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);

            ActionResult<ClassicCitySetupSessionView> actionResult = await controller.Update(
                sessionId: sessionId,
                request: request,
                cancellationToken: CancellationToken.None);

            BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.InvalidPayload",
                actual: GetAnonymousProperty<string>(
                    source: badRequest.Value,
                    propertyName: "code"));
            Assert.Equal(
                expected: "Draft is invalid.",
                actual: GetAnonymousProperty<string>(
                    source: badRequest.Value,
                    propertyName: "message"));
            Assert.Equal(
                expected: sessionId,
                actual: service.LastUpdateSessionId);
            Assert.Same(
                expected: request,
                actual: service.LastUpdateRequest);
        }

        [Fact]
        public async Task Launch_WhenServiceReturnsUpdated_ReturnsAcceptedWithSession()
        {
            var sessionId = Guid.Parse("122c3db8-e80e-42f5-b05a-0fe4300d9fa5");
            ClassicCitySetupSessionView session = CreateClassicCitySetupSessionView(
                sessionId: sessionId,
                status: "LaunchQueued",
                launchQueuedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 13,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
            var service = new RecordingClassicCitySetupSessionService
            {
                QueueLaunchResult = new ClassicCitySetupSessionMutationResult(
                    Status: ClassicCitySetupSessionMutationStatus.Updated,
                    Session: session,
                    ErrorCode: null,
                    ErrorMessage: null)
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);

            ActionResult<ClassicCitySetupSessionView> actionResult = await controller.Launch(
                sessionId: sessionId,
                cancellationToken: CancellationToken.None);

            AcceptedResult accepted = Assert.IsType<AcceptedResult>(actionResult.Result);
            ClassicCitySetupSessionView result = Assert.IsType<ClassicCitySetupSessionView>(accepted.Value);
            Assert.Equal(
                expected: "LaunchQueued",
                actual: result.Status);
            Assert.Equal(
                expected: sessionId,
                actual: service.LastQueueLaunchSessionId);
        }

        [Fact]
        public async Task Launch_WhenServiceReturnsUnavailable_MapsServiceUnavailablePayload()
        {
            var sessionId = Guid.Parse("01d2d693-9f70-46a1-a1d3-0427f9fe52e6");
            var service = new RecordingClassicCitySetupSessionService
            {
                QueueLaunchResult = new ClassicCitySetupSessionMutationResult(
                    Status: ClassicCitySetupSessionMutationStatus.Unavailable,
                    Session: null,
                    ErrorCode: "Gateway.ClassicCitySetup.SessionLockUnavailable",
                    ErrorMessage: "Lock backend is unavailable.")
            };
            ClassicCitySetupSessionsController controller = CreateClassicCitySetupSessionsController(service);

            ActionResult<ClassicCitySetupSessionView> actionResult = await controller.Launch(
                sessionId: sessionId,
                cancellationToken: CancellationToken.None);

            ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(
                expected: 503,
                actual: objectResult.StatusCode);
            Assert.Equal(
                expected: "Gateway.ClassicCitySetup.SessionLockUnavailable",
                actual: GetAnonymousProperty<string>(
                    source: objectResult.Value,
                    propertyName: "code"));
            Assert.Equal(
                expected: "Lock backend is unavailable.",
                actual: GetAnonymousProperty<string>(
                    source: objectResult.Value,
                    propertyName: "message"));
        }

        private static T GetAnonymousProperty<T>(
            object? source,
            string propertyName)
        {
            object? value = source?.GetType()
               .GetProperty(propertyName)
              ?.GetValue(source);
            return Assert.IsType<T>(value);
        }
    }
}
