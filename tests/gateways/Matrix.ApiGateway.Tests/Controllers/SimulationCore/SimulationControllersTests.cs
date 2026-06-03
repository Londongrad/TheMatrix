using Matrix.ApiGateway.Contracts.SimulationCore.Simulation.Requests;
using Matrix.ApiGateway.Controllers.SimulationCore.Simulation;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore
{
    public sealed class SimulationControllersTests
    {
        [Fact]
        public async Task SimulationsGetClock_WhenCalled_ReturnsOkClock()
        {
            var simulationId = Guid.Parse("4cbf64fb-79fd-489c-8b52-6be386150927");
            SimulationClockView clock = CreateSimulationClockView(simulationId: simulationId);
            var simulationClient = new RecordingSimulationApiClient
            {
                ClockResult = clock
            };
            SimulationsController controller = CreateSimulationsController(simulationClient);

            ActionResult<SimulationClockView?> actionResult = await controller.GetClock(
                simulationId: simulationId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Same(
                expected: clock,
                actual: Assert.IsType<SimulationClockView>(ok.Value));
            Assert.Equal(
                expected: simulationId,
                actual: simulationClient.LastClockSimulationId);
        }

        [Fact]
        public async Task SimulationsSetClockSpeed_WhenCalled_MapsRequest()
        {
            var simulationId = Guid.Parse("52007090-b078-47af-91b0-dd97f8078f45");
            var simulationClient = new RecordingSimulationApiClient();
            SimulationsController controller = CreateSimulationsController(simulationClient);

            IActionResult result = await controller.SetClockSpeed(
                simulationId: simulationId,
                request: new SetSimulationClockSpeedRequestDto
                {
                    Multiplier = 2.75m
                },
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(
                expected: simulationId,
                actual: simulationClient.LastSetSpeedSimulationId);
            Assert.NotNull(simulationClient.LastSetSpeedRequest);
            Assert.Equal(
                expected: 2.75m,
                actual: simulationClient.LastSetSpeedRequest!.Multiplier);
        }

        [Fact]
        public async Task SimulationsJumpClock_WhenCalled_MapsRequest()
        {
            var simulationId = Guid.Parse("d25ebce8-5611-4af3-8b1f-e919f97f89cb");
            DateTimeOffset newSimTimeUtc = new(
                year: 2048,
                month: 6,
                day: 5,
                hour: 14,
                minute: 45,
                second: 0,
                offset: TimeSpan.Zero);
            var simulationClient = new RecordingSimulationApiClient();
            SimulationsController controller = CreateSimulationsController(simulationClient);

            IActionResult result = await controller.JumpClock(
                simulationId: simulationId,
                request: new JumpSimulationClockRequestDto
                {
                    NewSimTimeUtc = newSimTimeUtc
                },
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(
                expected: simulationId,
                actual: simulationClient.LastJumpSimulationId);
            Assert.NotNull(simulationClient.LastJumpRequest);
            Assert.Equal(
                expected: newSimTimeUtc,
                actual: simulationClient.LastJumpRequest!.NewSimTimeUtc);
        }

    }
}
