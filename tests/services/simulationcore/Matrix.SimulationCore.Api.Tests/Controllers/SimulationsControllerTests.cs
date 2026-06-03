using Matrix.SimulationCore.Api.Controllers;
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

namespace Matrix.SimulationCore.Api.Tests.Controllers
{
    public sealed class SimulationsControllerTests
    {
        [Fact]
        public async Task GetClock_ReturnsMappedViewOrNotFound()
        {
            var simulationId = Guid.Parse("8d927d1e-9fe0-4d32-a177-e727e799b5e7");
            var sender = new FakeSender();
            sender.Handle<GetClockQuery, ClockDto?>(query =>
            {
                Assert.Equal(
                    expected: simulationId,
                    actual: query.SimulationId);
                return CreateClockDto(simulationId);
            });
            var controller = new SimulationsController(sender);

            IResult result = await controller.GetClock(
                simulationId: simulationId,
                cancellationToken: CancellationToken.None);
            SimulationClockView view = AssertResult<SimulationClockView>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: simulationId,
                actual: view.SimulationId);
            Assert.Equal(
                expected: "classic-city",
                actual: view.ScenarioKey);
            Assert.Equal(
                expected: "city",
                actual: view.HostTypeKey);
            Assert.Equal(
                expected: "Running",
                actual: view.State);

            var missingSender = new FakeSender();
            missingSender.Handle<GetClockQuery, ClockDto?>(_ => null);
            var missingController = new SimulationsController(missingSender);

            IResult missing = await missingController.GetClock(
                simulationId: simulationId,
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: missing,
                expectedStatusCode: StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task MutationEndpoints_ForwardCommandsAndMapStatuses()
        {
            var simulationId = Guid.Parse("8d927d1e-9fe0-4d32-a177-e727e799b5e7");
            DateTimeOffset newSimTimeUtc = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 12,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            var sender = new FakeSender();
            sender.Handle<PauseClockCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: simulationId,
                    actual: command.SimulationId);
                return true;
            });
            sender.Handle<ResumeClockCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: simulationId,
                    actual: command.SimulationId);
                return false;
            });
            sender.Handle<SetClockSpeedCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: simulationId,
                    actual: command.SimulationId);
                Assert.Equal(
                    expected: 3.5m,
                    actual: command.Multiplier);
                return true;
            });
            sender.Handle<JumpClockCommand, bool>(command =>
            {
                Assert.Equal(
                    expected: simulationId,
                    actual: command.SimulationId);
                Assert.Equal(
                    expected: newSimTimeUtc,
                    actual: command.NewSimTimeUtc);
                return true;
            });
            var controller = new SimulationsController(sender);

            IResult pause = await controller.Pause(
                simulationId: simulationId,
                cancellationToken: CancellationToken.None);
            IResult resume = await controller.Resume(
                simulationId: simulationId,
                cancellationToken: CancellationToken.None);
            IResult speed = await controller.SetSpeed(
                simulationId: simulationId,
                request: new SetSpeedRequest(3.5m),
                cancellationToken: CancellationToken.None);
            IResult jump = await controller.Jump(
                simulationId: simulationId,
                request: new JumpClockRequest(newSimTimeUtc),
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: pause,
                expectedStatusCode: StatusCodes.Status200OK);
            AssertStatus(
                result: resume,
                expectedStatusCode: StatusCodes.Status404NotFound);
            AssertStatus(
                result: speed,
                expectedStatusCode: StatusCodes.Status200OK);
            AssertStatus(
                result: jump,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Collection(
                collection: sender.Requests,
                request => Assert.IsType<PauseClockCommand>(request),
                request => Assert.IsType<ResumeClockCommand>(request),
                request => Assert.IsType<SetClockSpeedCommand>(request),
                request => Assert.IsType<JumpClockCommand>(request));
        }

    }
}
