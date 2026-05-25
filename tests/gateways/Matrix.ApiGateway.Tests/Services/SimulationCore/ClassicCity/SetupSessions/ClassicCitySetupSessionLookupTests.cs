using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupSessionLookupTests
    {
        [Fact]
        public async Task GetAsync_WhenSessionBelongsToCurrentUser_ReturnsView()
        {
            var ownerUserId = Guid.Parse("9e5ee042-f6cc-4f72-a8d5-a958481511fd");
            var sessionId = Guid.Parse("bb3ca890-1793-406b-adf6-b722d43fa77b");
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

            ClassicCitySetupSessionView? result = await service.GetAsync(sessionId);

            Assert.NotNull(result);
            Assert.Equal(
                expected: sessionId,
                actual: result!.SessionId);
            Assert.Equal(
                expected: "Draft",
                actual: result.Status);
        }

        [Fact]
        public async Task GetAsync_WhenSessionBelongsToAnotherUser_ReturnsNull()
        {
            var ownerUserId = Guid.Parse("0ee376d2-84e2-4888-b45e-1129b46a80d3");
            var sessionId = Guid.Parse("b1ac0ef5-fe5e-412a-bda7-c376801ffbb8");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            sessionStore.Sessions[sessionId] = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: Guid.Parse("f9d35f15-fdd9-4f8d-bac4-2ea8bc797f7e"),
                status: "Draft");
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            ClassicCitySetupSessionView? result = await service.GetAsync(sessionId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_WhenSessionHasNoOwner_AllowsCurrentUser()
        {
            var ownerUserId = Guid.Parse("8b2cc6fb-24ce-4208-ba6d-230c76346440");
            var sessionId = Guid.Parse("80ee12e5-a28c-4a87-9958-b2b2f90efc46");
            var sessionStore = new FakeClassicCitySetupSessionStore();
            var publishEndpoint = new RecordingPublishEndpoint();
            ClassicCitySetupSessionState session = CreateClassicCitySetupSessionState(
                sessionId: sessionId,
                ownerUserId: ownerUserId,
                status: "Draft");
            session.OwnerUserId = null;
            sessionStore.Sessions[sessionId] = session;
            ClassicCitySetupSessionService service = CreateClassicCitySetupSessionService(
                sessionStore: sessionStore,
                publishEndpoint: publishEndpoint,
                httpContextAccessor: CreateHttpContextAccessor(ownerUserId));

            ClassicCitySetupSessionView? result = await service.GetAsync(sessionId);

            Assert.NotNull(result);
            Assert.Equal(
                expected: sessionId,
                actual: result!.SessionId);
        }
    }
}
