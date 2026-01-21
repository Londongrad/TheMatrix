using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Infrastructure.Public
{
    public sealed record CityClockView(
        Guid CityId,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        ClockState State);
}
