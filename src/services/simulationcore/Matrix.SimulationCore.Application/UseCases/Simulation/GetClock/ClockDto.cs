using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.GetClock
{
    public sealed record ClockDto(
        Guid SimulationId,
        Guid HostId,
        string HostKind,
        string SimulationKind,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        ClockState State)
    {
        public static ClockDto FromDomain(
            SimulationClock clock,
            SimulationHost host,
            bool forcePaused = false)
        {
            return new ClockDto(
                SimulationId: clock.SimulationId.Value,
                HostId: host.HostId.Value,
                HostKind: host.RuntimeKey.HostTypeKey.Value,
                SimulationKind: host.RuntimeKey.ScenarioKey.Value,
                SimTimeUtc: clock.CurrentTime.ValueUtc,
                TickId: clock.TickId.Value,
                Speed: clock.Speed.Multiplier,
                State: forcePaused
                    ? ClockState.Paused
                    : clock.State);
        }
    }
}
