using Matrix.SimulationCore.Api.Controllers;
using Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity.Simulation;
using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.PauseClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock;
using Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed;
using Matrix.SimulationCore.Contracts.Simulation.Requests;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationCore.Api.Tests.TestSupport.SimulationCoreApiTestSupport;

namespace Matrix.SimulationCore.Api.Tests.Controllers;

public sealed class SimulationsControllerTests
{
    [Fact]
    public async Task GetClock_ReturnsMappedViewOrNotFound()
    {
        Guid simulationId = Guid.Parse("8d927d1e-9fe0-4d32-a177-e727e799b5e7");
        var sender = new FakeSender();
        sender.Handle<GetClockQuery, ClockDto?>(query =>
        {
            Assert.Equal(simulationId, query.SimulationId);
            return CreateClockDto(simulationId);
        });
        var controller = new SimulationsController(sender);

        IResult result = await controller.GetClock(simulationId, CancellationToken.None);
        SimulationClockView view = AssertResult<SimulationClockView>(result, StatusCodes.Status200OK);

        Assert.Equal(simulationId, view.SimulationId);
        Assert.Equal("Running", view.State);

        var missingSender = new FakeSender();
        missingSender.Handle<GetClockQuery, ClockDto?>(_ => null);
        var missingController = new SimulationsController(missingSender);

        IResult missing = await missingController.GetClock(simulationId, CancellationToken.None);

        AssertStatus(missing, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task MutationEndpoints_ForwardCommandsAndMapStatuses()
    {
        Guid simulationId = Guid.Parse("8d927d1e-9fe0-4d32-a177-e727e799b5e7");
        DateTimeOffset newSimTimeUtc = new(2048, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var sender = new FakeSender();
        sender.Handle<PauseClockCommand, bool>(command =>
        {
            Assert.Equal(simulationId, command.SimulationId);
            return true;
        });
        sender.Handle<ResumeClockCommand, bool>(command =>
        {
            Assert.Equal(simulationId, command.SimulationId);
            return false;
        });
        sender.Handle<SetClockSpeedCommand, bool>(command =>
        {
            Assert.Equal(simulationId, command.SimulationId);
            Assert.Equal(3.5m, command.Multiplier);
            return true;
        });
        sender.Handle<JumpClockCommand, bool>(command =>
        {
            Assert.Equal(simulationId, command.SimulationId);
            Assert.Equal(newSimTimeUtc, command.NewSimTimeUtc);
            return true;
        });
        var controller = new SimulationsController(sender);

        IResult pause = await controller.Pause(simulationId, CancellationToken.None);
        IResult resume = await controller.Resume(simulationId, CancellationToken.None);
        IResult speed = await controller.SetSpeed(simulationId, new SetSpeedRequest(3.5m), CancellationToken.None);
        IResult jump = await controller.Jump(simulationId, new JumpClockRequest(newSimTimeUtc), CancellationToken.None);

        AssertStatus(pause, StatusCodes.Status200OK);
        AssertStatus(resume, StatusCodes.Status404NotFound);
        AssertStatus(speed, StatusCodes.Status200OK);
        AssertStatus(jump, StatusCodes.Status200OK);
        Assert.Collection(
            sender.Requests,
            request => Assert.IsType<PauseClockCommand>(request),
            request => Assert.IsType<ResumeClockCommand>(request),
            request => Assert.IsType<SetClockSpeedCommand>(request),
            request => Assert.IsType<JumpClockCommand>(request));
    }

    [Fact]
    public async Task CitySimulationEndpoints_UseCityIdentifierAsSimulationIdentifier()
    {
        Guid cityId = Guid.Parse("fd72808b-2cb0-4dd2-bf3e-d542409ef2f7");
        DateTimeOffset newSimTimeUtc = new(2048, 6, 1, 13, 0, 0, TimeSpan.Zero);
        var sender = new FakeSender();
        sender.Handle<GetClockQuery, ClockDto?>(query =>
        {
            Assert.Equal(cityId, query.SimulationId);
            return CreateClockDto(cityId);
        });
        sender.Handle<PauseClockCommand, bool>(command =>
        {
            Assert.Equal(cityId, command.SimulationId);
            return true;
        });
        sender.Handle<ResumeClockCommand, bool>(command =>
        {
            Assert.Equal(cityId, command.SimulationId);
            return true;
        });
        sender.Handle<SetClockSpeedCommand, bool>(command =>
        {
            Assert.Equal(cityId, command.SimulationId);
            Assert.Equal(1.25m, command.Multiplier);
            return true;
        });
        sender.Handle<JumpClockCommand, bool>(command =>
        {
            Assert.Equal(cityId, command.SimulationId);
            Assert.Equal(newSimTimeUtc, command.NewSimTimeUtc);
            return true;
        });
        var controller = new SimulationController(sender);

        IResult get = await controller.GetClock(cityId, CancellationToken.None);
        IResult pause = await controller.Pause(cityId, CancellationToken.None);
        IResult resume = await controller.Resume(cityId, CancellationToken.None);
        IResult speed = await controller.SetSpeed(cityId, new SetSpeedRequest(1.25m), CancellationToken.None);
        IResult jump = await controller.Jump(cityId, new JumpClockRequest(newSimTimeUtc), CancellationToken.None);

        SimulationClockView view = AssertResult<SimulationClockView>(get, StatusCodes.Status200OK);
        Assert.Equal(cityId, view.SimulationId);
        AssertStatus(pause, StatusCodes.Status200OK);
        AssertStatus(resume, StatusCodes.Status200OK);
        AssertStatus(speed, StatusCodes.Status200OK);
        AssertStatus(jump, StatusCodes.Status200OK);
    }
}
