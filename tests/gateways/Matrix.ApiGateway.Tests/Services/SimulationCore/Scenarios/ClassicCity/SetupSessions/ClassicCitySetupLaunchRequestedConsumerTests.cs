using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.ApiGateway.Tests.TestSupport;
using Xunit;

namespace Matrix.ApiGateway.Tests.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions
{
    public sealed class ClassicCitySetupLaunchRequestedConsumerTests
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
    }
}
