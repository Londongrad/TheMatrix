using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation;

internal static class SimulationTestSupport
{
    internal static readonly DateTimeOffset SimStartTimeUtc = new(2048, 1, 2, 3, 4, 5, TimeSpan.Zero);

    internal static SimulationClock CreateClock(
        Guid? simulationId = null,
        ClockState state = ClockState.Running,
        decimal speed = 60m)
    {
        var cityId = new CityId(simulationId ?? Guid.NewGuid());
        return SimulationClock.Create(
            cityId: cityId,
            startTime: SimTime.FromUtc(SimStartTimeUtc),
            speed: SimSpeed.From(speed),
            initialState: state);
    }

    internal static SimulationHost CreateHost(
        Guid? simulationId = null,
        SimulationHostState state = SimulationHostState.Active)
    {
        var id = simulationId ?? Guid.NewGuid();
        return new SimulationHost(
            SimulationId: new SimulationId(id),
            HostId: new SimulationHostId(id),
            HostKind: SimulationHostKind.City,
            SimulationKind: SimulationKind.ClassicCity,
            State: state,
            CreatedAtUtc: SimStartTimeUtc.AddHours(-1),
            ArchivedAtUtc: state == SimulationHostState.Archived ? SimStartTimeUtc : null);
    }

    internal sealed class FakeSimulationClockRepository : ISimulationClockRepository
    {
        public SimulationClock? ClockBySimulationId { get; set; }
        public SimulationId? RequestedSimulationId { get; private set; }

        public Task<SimulationClock?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationId = simulationId;
            return Task.FromResult(ClockBySimulationId);
        }

        public Task AddAsync(SimulationClock clock, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteBySimulationIdAsync(SimulationId simulationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeSimulationHostReadRepository : ISimulationHostReadRepository
    {
        public SimulationHost? HostBySimulationId { get; set; }
        public SimulationId? RequestedSimulationId { get; private set; }

        public Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            RequestedSimulationId = simulationId;
            return Task.FromResult(HostBySimulationId);
        }
    }
}
