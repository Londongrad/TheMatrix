using Matrix.CityCore.Domain.Time;

namespace Matrix.CityCore.Application.UseCases.Simulation.GetClock
{
    public sealed record ClockDto(
        Guid CityId,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        ClockState State)
    {
        public static ClockDto FromDomain(SimulationClock clock)
        {
            return new ClockDto(
                CityId: clock.Id.Value,
                SimTimeUtc: clock.CurrentTime.ValueUtc,
                TickId: clock.TickId.Value,
                Speed: clock.Speed.Multiplier,
                State: clock.State);
        }
    }
}
