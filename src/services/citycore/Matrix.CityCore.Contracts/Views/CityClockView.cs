namespace Matrix.CityCore.Contracts.Views
{
    public sealed record CityClockView(
        Guid CityId,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        string State);
}
