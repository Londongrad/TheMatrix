using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
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
            string simulationKind,
            bool forcePaused = false)
        {
            return new ClockDto(
                SimulationId: clock.SimulationId.Value,
                HostId: clock.Id.Value,
                HostKind: "city",
                SimulationKind: simulationKind,
                SimTimeUtc: clock.CurrentTime.ValueUtc,
                TickId: clock.TickId.Value,
                Speed: clock.Speed.Multiplier,
                State: forcePaused
                    ? ClockState.Paused
                    : clock.State);
        }
    }
}
