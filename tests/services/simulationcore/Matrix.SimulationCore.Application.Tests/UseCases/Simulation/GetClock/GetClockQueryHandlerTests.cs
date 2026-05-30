using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.GetClock
{
    public sealed class GetClockQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenHostDoesNotExist_ReturnsNull()
        {
            var simulationId = Guid.NewGuid();
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository();
            var handler = new GetClockQueryHandler(
                repository: clockRepository,
                simulationHostRepository: hostRepository);

            ClockDto? result = await handler.Handle(
                request: new GetClockQuery(simulationId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: simulationId,
                actual: hostRepository.RequestedSimulationId!.Value.Value);
            Assert.Null(clockRepository.RequestedSimulationId);
        }

        [Fact]
        public async Task Handle_WhenClockDoesNotExist_ReturnsNull()
        {
            var simulationId = Guid.NewGuid();
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository
            {
                HostBySimulationId = SimulationTestSupport.CreateHost(simulationId)
            };
            var handler = new GetClockQueryHandler(
                repository: clockRepository,
                simulationHostRepository: hostRepository);

            ClockDto? result = await handler.Handle(
                request: new GetClockQuery(simulationId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: simulationId,
                actual: hostRepository.RequestedSimulationId!.Value.Value);
            Assert.Equal(
                expected: simulationId,
                actual: clockRepository.RequestedSimulationId!.Value.Value);
        }

        [Fact]
        public async Task Handle_WhenHostIsActive_ReturnsClockDto()
        {
            var simulationId = Guid.NewGuid();
            var hostId = Guid.NewGuid();
            SimulationClock clock = SimulationTestSupport.CreateClock(
                simulationId: simulationId,
                state: ClockState.Running,
                speed: 60m);
            SimulationHost host = SimulationTestSupport.CreateHost(
                simulationId: simulationId,
                state: SimulationHostState.Active) with
            {
                HostId = new SimulationHostId(hostId)
            };
            var handler = new GetClockQueryHandler(
                repository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = clock
                },
                simulationHostRepository: new SimulationTestSupport.FakeSimulationHostReadRepository
                {
                    HostBySimulationId = host
                });

            ClockDto? result = await handler.Handle(
                request: new GetClockQuery(simulationId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: simulationId,
                actual: result!.SimulationId);
            Assert.Equal(
                expected: hostId,
                actual: result.HostId);
            Assert.Equal(
                expected: "city",
                actual: result.HostTypeKey);
            Assert.Equal(
                expected: "classic-city",
                actual: result.ScenarioKey);
            Assert.Equal(
                expected: SimulationTestSupport.SimStartTimeUtc,
                actual: result.SimTimeUtc);
            Assert.Equal(
                expected: 0L,
                actual: result.TickId);
            Assert.Equal(
                expected: 60m,
                actual: result.Speed);
            Assert.Equal(
                expected: ClockState.Running,
                actual: result.State);
        }

        [Fact]
        public async Task Handle_WhenHostIsArchived_ForcesPausedState()
        {
            var simulationId = Guid.NewGuid();
            SimulationClock clock = SimulationTestSupport.CreateClock(
                simulationId: simulationId,
                state: ClockState.Running,
                speed: 5m);
            SimulationHost host = SimulationTestSupport.CreateHost(
                simulationId: simulationId,
                state: SimulationHostState.Archived);
            var handler = new GetClockQueryHandler(
                repository: new SimulationTestSupport.FakeSimulationClockRepository
                {
                    ClockBySimulationId = clock
                },
                simulationHostRepository: new SimulationTestSupport.FakeSimulationHostReadRepository
                {
                    HostBySimulationId = host
                });

            ClockDto? result = await handler.Handle(
                request: new GetClockQuery(simulationId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: ClockState.Paused,
                actual: result!.State);
            Assert.Equal(
                expected: 5m,
                actual: result.Speed);
        }
    }
}
