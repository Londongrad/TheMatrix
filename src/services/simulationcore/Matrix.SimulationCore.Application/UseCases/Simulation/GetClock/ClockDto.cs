using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.GetClock
{
    public sealed record ClockDto(
        Guid SimulationId,
        Guid HostId,
        string ScenarioKey,
        string HostTypeKey,
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
                ScenarioKey: host.RuntimeKey.ScenarioKey.Value,
                HostTypeKey: host.RuntimeKey.HostTypeKey.Value,
                SimTimeUtc: clock.CurrentTime.ValueUtc,
                TickId: clock.TickId.Value,
                Speed: clock.Speed.Multiplier,
                State: forcePaused
                    ? ClockState.Paused
                    : clock.State);
        }
    }
}
