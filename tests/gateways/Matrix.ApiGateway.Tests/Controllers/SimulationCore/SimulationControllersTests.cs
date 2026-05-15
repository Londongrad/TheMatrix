using Matrix.ApiGateway.Contracts.SimulationCore.Simulation.Requests;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore;

public sealed class SimulationControllersTests
{
    [Fact]
    public async Task SimulationsGetClock_WhenCalled_ReturnsOkClock()
    {
        Guid simulationId = Guid.Parse("4cbf64fb-79fd-489c-8b52-6be386150927");
        SimulationClockView clock = CreateSimulationClockView(simulationId: simulationId);
        var simulationClient = new RecordingSimulationApiClient
        {
            ClockResult = clock
        };
        var controller = CreateSimulationsController(simulationClient);

        ActionResult<SimulationClockView?> actionResult = await controller.GetClock(simulationId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Same(clock, Assert.IsType<SimulationClockView>(ok.Value));
        Assert.Equal(simulationId, simulationClient.LastClockSimulationId);
    }

    [Fact]
    public async Task SimulationsSetClockSpeed_WhenCalled_MapsRequest()
    {
        Guid simulationId = Guid.Parse("52007090-b078-47af-91b0-dd97f8078f45");
        var simulationClient = new RecordingSimulationApiClient();
        var controller = CreateSimulationsController(simulationClient);

        IActionResult result = await controller.SetClockSpeed(
            simulationId: simulationId,
            request: new SetSimulationClockSpeedRequestDto
            {
                Multiplier = 2.75m
            },
            cancellationToken: CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(simulationId, simulationClient.LastSetSpeedSimulationId);
        Assert.NotNull(simulationClient.LastSetSpeedRequest);
        Assert.Equal(2.75m, simulationClient.LastSetSpeedRequest!.Multiplier);
    }

    [Fact]
    public async Task SimulationsJumpClock_WhenCalled_MapsRequest()
    {
        Guid simulationId = Guid.Parse("d25ebce8-5611-4af3-8b1f-e919f97f89cb");
        DateTimeOffset newSimTimeUtc = new(2048, 6, 5, 14, 45, 0, TimeSpan.Zero);
        var simulationClient = new RecordingSimulationApiClient();
        var controller = CreateSimulationsController(simulationClient);

        IActionResult result = await controller.JumpClock(
            simulationId: simulationId,
            request: new JumpSimulationClockRequestDto
            {
                NewSimTimeUtc = newSimTimeUtc
            },
            cancellationToken: CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(simulationId, simulationClient.LastJumpSimulationId);
        Assert.NotNull(simulationClient.LastJumpRequest);
        Assert.Equal(newSimTimeUtc, simulationClient.LastJumpRequest!.NewSimTimeUtc);
    }

    [Fact]
    public async Task CitySimulationGetClock_WhenCalled_ReturnsOkClock()
    {
        Guid cityId = Guid.Parse("dba47d3f-9b60-4603-82f6-e240f75263b2");
        SimulationClockView clock = CreateSimulationClockView(simulationId: cityId);
        var simulationClient = new RecordingSimulationApiClient
        {
            ClockResult = clock
        };
        var controller = CreateCitySimulationController(simulationClient);

        ActionResult<SimulationClockView?> actionResult = await controller.GetClock(cityId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Same(clock, Assert.IsType<SimulationClockView>(ok.Value));
        Assert.Equal(cityId, simulationClient.LastClockSimulationId);
    }

    [Fact]
    public async Task CitySimulationPauseClock_WhenCalled_ReturnsNoContent()
    {
        Guid cityId = Guid.Parse("af5d6360-8730-4c8f-a1ac-a3f11eebbc0f");
        var simulationClient = new RecordingSimulationApiClient();
        var controller = CreateCitySimulationController(simulationClient);

        IActionResult result = await controller.PauseClock(cityId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(cityId, simulationClient.LastPausedSimulationId);
    }

    [Fact]
    public async Task CitySimulationResumeClock_WhenCalled_ReturnsNoContent()
    {
        Guid cityId = Guid.Parse("25226df5-d0c5-4535-a666-9bf6c15fdce2");
        var simulationClient = new RecordingSimulationApiClient();
        var controller = CreateCitySimulationController(simulationClient);

        IActionResult result = await controller.ResumeClock(cityId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(cityId, simulationClient.LastResumedSimulationId);
    }
}
