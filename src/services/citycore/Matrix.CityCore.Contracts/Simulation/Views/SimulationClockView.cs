namespace Matrix.CityCore.Contracts.Simulation.Views
{
    public sealed record SimulationClockView(
        Guid CityId,
        DateTimeOffset SimTimeUtc,
        long TickId,
        decimal Speed,
        string State);
}
