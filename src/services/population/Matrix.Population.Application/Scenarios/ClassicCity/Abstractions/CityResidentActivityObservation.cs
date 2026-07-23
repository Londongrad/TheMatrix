namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;

public sealed record CityResidentActivityObservation(long SourceTickId, DateTimeOffset ObservedAtSimTimeUtc)
{
    public static CityResidentActivityObservation? ForTick(long tickId, DateTimeOffset fromUtc,
        DateTimeOffset toUtc, bool isInitialTick)
    {
        if (tickId < 0 || fromUtc.Offset != TimeSpan.Zero || toUtc.Offset != TimeSpan.Zero || toUtc < fromUtc)
            throw new ArgumentException("Invalid activity observation tick.");
        return isInitialTick || fromUtc.UtcTicks / TimeSpan.TicksPerHour != toUtc.UtcTicks / TimeSpan.TicksPerHour
            ? new(tickId, toUtc) : null;
    }
}
