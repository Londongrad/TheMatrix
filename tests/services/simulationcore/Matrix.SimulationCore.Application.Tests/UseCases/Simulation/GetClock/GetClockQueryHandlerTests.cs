using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.GetClock;

public sealed class GetClockQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenHostDoesNotExist_ReturnsNull()
    {
        Guid simulationId = Guid.NewGuid();
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository();
        var handler = new GetClockQueryHandler(clockRepository, hostRepository);

        var result = await handler.Handle(new GetClockQuery(simulationId), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(simulationId, hostRepository.RequestedSimulationId!.Value.Value);
        Assert.Null(clockRepository.RequestedSimulationId);
    }

    [Fact]
    public async Task Handle_WhenClockDoesNotExist_ReturnsNull()
    {
        Guid simulationId = Guid.NewGuid();
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var hostRepository = new SimulationTestSupport.FakeSimulationHostReadRepository
        {
            HostBySimulationId = SimulationTestSupport.CreateHost(simulationId)
        };
        var handler = new GetClockQueryHandler(clockRepository, hostRepository);

        var result = await handler.Handle(new GetClockQuery(simulationId), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(simulationId, hostRepository.RequestedSimulationId!.Value.Value);
        Assert.Equal(simulationId, clockRepository.RequestedSimulationId!.Value.Value);
    }

    [Fact]
    public async Task Handle_WhenHostIsActive_ReturnsClockDto()
    {
        Guid simulationId = Guid.NewGuid();
        var clock = SimulationTestSupport.CreateClock(simulationId, state: ClockState.Running, speed: 60m);
        var host = SimulationTestSupport.CreateHost(simulationId, state: SimulationHostState.Active);
        var handler = new GetClockQueryHandler(
            new SimulationTestSupport.FakeSimulationClockRepository
            {
                ClockBySimulationId = clock
            },
            new SimulationTestSupport.FakeSimulationHostReadRepository
            {
                HostBySimulationId = host
            });

        var result = await handler.Handle(new GetClockQuery(simulationId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(simulationId, result!.SimulationId);
        Assert.Equal(simulationId, result.HostId);
        Assert.Equal("City", result.HostKind);
        Assert.Equal("ClassicCity", result.SimulationKind);
        Assert.Equal(SimulationTestSupport.SimStartTimeUtc, result.SimTimeUtc);
        Assert.Equal(0L, result.TickId);
        Assert.Equal(60m, result.Speed);
        Assert.Equal(ClockState.Running, result.State);
    }

    [Fact]
    public async Task Handle_WhenHostIsArchived_ForcesPausedState()
    {
        Guid simulationId = Guid.NewGuid();
        var clock = SimulationTestSupport.CreateClock(simulationId, state: ClockState.Running, speed: 5m);
        var host = SimulationTestSupport.CreateHost(simulationId, state: SimulationHostState.Archived);
        var handler = new GetClockQueryHandler(
            new SimulationTestSupport.FakeSimulationClockRepository
            {
                ClockBySimulationId = clock
            },
            new SimulationTestSupport.FakeSimulationHostReadRepository
            {
                HostBySimulationId = host
            });

        var result = await handler.Handle(new GetClockQuery(simulationId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ClockState.Paused, result!.State);
        Assert.Equal(5m, result.Speed);
    }
}
